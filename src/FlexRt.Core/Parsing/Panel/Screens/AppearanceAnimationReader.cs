using System.Numerics;
using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Appearance I/O field: ADAPTDYNATTRIBU → SYMB с первой ссылкой на VAR; сверено на A603A0097 и опытах V21.</summary>
internal static class AppearanceAnimationReader
{
    /// <summary>Начало диапазонов после u16 количества и u16 режима.</summary>
    private const int RangesOffset = 0x04;

    /// <summary>Размер диапазона Range: два u32.</summary>
    private const int RangeSize = 0x08;

    /// <summary>Размер заголовка перед цветами; на +2 размер записи, остальные байты не интерпретируются.</summary>
    private const int ColorsHeaderSize = 0x04;

    /// <summary>Размер записи цвета: RGBA текста, RGBA фона, флаги мигания.</summary>
    private const int ColorSize = 0x09;

    /// <summary>Найти Appearance; повреждённая ссылка даёт предупреждение и непрочитанные настройки, поле сохраняется.</summary>
    /// <returns>Настройки; ссылки нет — null, сбой — Ranges null.</returns>
    public static AppearanceAnimation? Read(FwxDocument doc, TocEntry owner, TableItem field, long end, IReadOnlyList<VarReference> links)
    {
        try
        {
            return ReadCore(doc, owner, field, end, links);
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add($"{e.Message} (Appearance не прочитана)");
            return new AppearanceAnimation(null, null, null);
        }
    }

    /// <summary>Проследить ссылки Appearance; ошибка цепочки не перехватывается.</summary>
    /// <returns>Анимация; цепочки нет — null.</returns>
    /// <exception cref="FwxFormatException">Ссылка или элемент Appearance не сходится.</exception>
    private static AppearanceAnimation? ReadCore(FwxDocument doc, TocEntry owner, TableItem field, long fieldEnd, IReadOnlyList<VarReference> links)
    {
        for (var i = 0; i < links.Count; i++)
        {
            var link = links[i];
            var source = field.Offset + fieldEnd - (links.Count - i) * 8L;
            var dynamic = doc.FindTable(link.Table);
            // Служебные слова (1, 1) — ссылка поля на ADAPTDYNATTRIBU, в том числе Appearance (шаги 01–05, 25).
            if (dynamic is null && link.Table != 0 && doc.Binary.D2(source + 0x04) == 1 && doc.Binary.D2(source + 0x06) == 1)
            {
                dynamic = AnimationLinkReader.Table(doc, link, owner.Name, source);
            }
            if (dynamic?.Name != "ADAPTDYNATTRIBU")
            {
                continue;
            }
            var dynamicItem = ElementLink.Item(doc, dynamic, link.Index);
            var (_, targets) = ReferenceHeader.Read(doc.Binary, dynamicItem, dynamic.Name);
            for (var j = 0; j < targets.Count; j++)
            {
                var target = targets[j];
                var table = AnimationLinkReader.Table(doc, target, dynamic.Name, dynamicItem.Offset + 0x08 + j * 8L);
                if (table.Name != "SYMB")
                {
                    continue;
                }
                var item = ElementLink.Item(doc, table, target.Index);
                var (end, references) = ReferenceHeader.Read(doc.Binary, item, table.Name);
                if (references.Count == 0)
                {
                    continue;
                }
                var tagTable = AnimationLinkReader.Table(doc, references[0], table.Name, item.Offset + 0x08);
                if (tagTable.Name != "VAR")
                {
                    continue;
                }
                ElementLink.Item(doc, tagTable, references[0].Index);
                return ReadRecord(doc, table, item, end, references[0]);
            }
        }
        return null;
    }

    /// <summary>Range: N пар u32; Single bit: инвертированная маска бита. Далее обычный вид и N записей цветов.</summary>
    /// <returns>Настройки Appearance.</returns>
    /// <exception cref="FwxFormatException">Неизвестный режим или диапазоны и цвета не помещаются в элемент.</exception>
    private static AppearanceAnimation ReadRecord(FwxDocument doc, TocEntry table, TableItem item, long end, VarReference tag)
    {
        var b = doc.Binary;
        Check(table, item, end, RangesOffset);
        var p = item.Offset + end;
        var count = b.D2(p);
        var mode = b.D2(p + 2);
        if (mode is not (1 or 4) || (mode == 4 && count != 2))
        {
            throw new FwxFormatException(table.Name, p, $"Appearance: режим {mode}, число диапазонов {count} не разобраны");
        }
        var rangesBytes = mode == 1 ? count * (long)RangeSize : 4;
        var header = end + RangesOffset + rangesBytes;
        Check(table, item, header, ColorsHeaderSize + (count + 1L) * ColorSize);
        if (b.D1(item.Offset + header + 2) != ColorSize)
        {
            throw new FwxFormatException(table.Name, item.Offset + header, "Appearance: размер записи цвета не равен 9");
        }
        var bit = mode == 4 ? Bit(table, item, p + RangesOffset, b.D4(p + RangesOffset)) : (int?)null;
        var colors = item.Offset + header + ColorsHeaderSize;
        var tagInfo = doc.Tags.FirstOrDefault(t => t.Index == tag.Index + 1);
        // У Real в V21 TP700 границы также целые i32; дробный ввод TIA не сохраняет (опыты 2026-11-14).
        var tagType = tagInfo?.TypeCode & ~HmiTag.ArrayFlag & ~0x80;
        if (mode == 1 && tagType is not (0x02 or 0x03 or 0x04 or 0x0b or 0x10 or 0x11 or 0x12 or 0x13))
        {
            throw new FwxFormatException(table.Name, p, "Appearance: границы Range для этого типа тега не разобраны");
        }
        var rows = Enumerable.Range(0, count)
            .Select(index => ReadRange(b, p + RangesOffset, colors, index, mode == 4, tagInfo)).ToArray();
        return new AppearanceAnimation(tagInfo?.Name, bit, rows);
    }

    /// <summary>Прочитать диапазон или состояние бита и соответствующую запись цветов после обычного вида поля.</summary>
    /// <returns>Диапазон Appearance с цветами и кодом мигания.</returns>
    private static AppearanceRange ReadRange(FwxBinary b, long ranges, long colors, int index, bool singleBit, HmiTag? tag)
    {
        var range = ranges + index * RangeSize;
        var color = colors + (index + 1L) * ColorSize;
        return new AppearanceRange(
            singleBit ? index : Bound(b, range, tag),
            singleBit ? index : Bound(b, range + 0x04, tag),
            StyleElement.Rgb(b, color),
            StyleElement.Rgb(b, color + 0x04),
            b.D1(color + 0x08));
    }

    /// <summary>Целая знаковая граница у Int, DInt, SInt и Real; бит 0x80 ПЛК и бит массива не участвуют в коде типа.</summary>
    /// <returns>Граница Range; отрицательные Real сверены на TP700 Comfort в TIA V21, 2026-11-14.</returns>
    private static long Bound(FwxBinary b, long offset, HmiTag? tag) =>
        (tag?.TypeCode & ~HmiTag.ArrayFlag & ~0x80) is 0x02 or 0x03 or 0x04 or 0x10 ? (int)b.D4(offset) : b.D4(offset);

    /// <summary>Единственный нулевой бит инвертированной маски Single bit.</summary>
    /// <returns>Номер бита 0…31.</returns>
    /// <exception cref="FwxFormatException">Маска не задаёт один бит.</exception>
    private static int Bit(TocEntry table, TableItem item, long offset, long mask)
    {
        var bits = ~(uint)mask;
        return BitOperations.IsPow2(bits)
            ? BitOperations.TrailingZeroCount(bits)
            : throw new FwxFormatException(table.Name, offset, $"запись {item.Index + 1}: маска Appearance не задаёт один бит");
    }

    /// <summary>Проверить границы диапазона внутри элемента SYMB.</summary>
    /// <exception cref="FwxFormatException">Диапазон выходит за элемент.</exception>
    private static void Check(TocEntry table, TableItem item, long offset, long length)
    {
        if (offset < 0 || length < 0 || offset + length > item.Length)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: данные Appearance не помещаются в {item.Length} байт");
        }
    }
}
