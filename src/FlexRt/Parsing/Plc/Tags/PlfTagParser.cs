using FlexRt.Binary;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;

namespace FlexRt.Parsing.Plc.Tags;

/// <summary>
/// Разбор объекта «тег ПЛК» (класс 0x0002105c) из PEData.plf. Опирается на слоты смещений
/// в заголовке объекта и на длины полей, а не на фиксированные позиции: так разбираются все
/// теги семи проверенных проектов (S7-1500 и S7-1200, разные авторы и версии TIA).
/// </summary>
internal static class PlfTagParser
{
    /// <summary>Класс объекта «тег ПЛК».</summary>
    private const long TagClass = 0x0002105c;

    /// <summary>Маркер перед списком связей.</summary>
    private const int RelationsMark = 0x54;

    /// <summary>Наблюдение: у разрешённого адреса в блоке доступа после ID символа идёт байт 01.</summary>
    private const int ResolvedFlag = 1;

    /// <summary>Наблюдение: длина блока типа = 0x39 + префикс строки типа.</summary>
    private const long TypeBlockBase = 0x39;

    /// <summary>
    /// Наблюдение: у второй формы блока типа (20 тегов в двух проектах) строка типа идёт
    /// после этих 12 байт, а не внутри блока.
    /// </summary>
    private static readonly byte[] SecondTypeFormMark = [0x0c, 0, 0, 0, 0, 0, 0, 0, 0x0c, 0, 0, 0];

    /// <summary>Наблюдение: начало второй формы блока типа после <c>[u32 0x39][01]</c>.</summary>
    private static readonly byte[] SecondTypeFormStart = [0x25, 0, 0x0c];

    /// <summary>В каком окне за блоком доступа ищется блок типа (наблюдение: в пределах 0x30).</summary>
    private const int TypeSearchWindow = 0x80;

    /// <summary>В каком окне от блока второй формы ищется <see cref="SecondTypeFormMark"/>.</summary>
    private const int SecondTypeFormWindow = 0x60;

    /// <summary>
    /// Прочитать все теги ПЛК проекта по возрастанию ID. Тег, который не удалось разобрать,
    /// попадает в <see cref="PlcProject.Problems"/> и считается в <see cref="PlcProject.UnparsedTags"/>,
    /// остальные читаются.
    /// </summary>
    public static void ReadAll(PlfFile file, PlcProject project)
    {
        foreach (var obj in file.OfClassById(TagClass))
        {
            try
            {
                project.Tags.Add(Read(file.Binary, obj));
            }
            catch (FwxFormatException e)
            {
                project.Problems.Add($"тег ПЛК, объект {obj.Id}: {e.Message}");
                project.UnparsedTags++;
            }
        }
    }

    /// <summary>
    /// Прочитать тег. Слоты заголовка объекта: <c>+0x38</c> начало и <c>+0x44</c> конец блока
    /// доступа, <c>+0x4c</c> начало секции имени, <c>+0x50</c> секция за ней, <c>+0x58</c>
    /// начало связей. Из секции имени — имя и комментарий, из связей — ПЛК (связи не должны
    /// выходить за объект), из блока доступа — ID символа, область и адрес, за ним — тип.
    /// </summary>
    /// <returns>Тег ПЛК.</returns>
    /// <exception cref="FwxFormatException">Раскладка объекта не сходится с известной.</exception>
    private static PlcTag Read(FwxBinary b, PlfObject obj)
    {
        var o = obj.Offset;
        var end = o + obj.Length;
        var access = o + Slot(b, obj, 0x38);
        var accessLimit = o + Slot(b, obj, 0x44);
        var names = o + Slot(b, obj, 0x4c);
        var afterNames = o + Slot(b, obj, 0x50);
        var relations = o + Slot(b, obj, 0x58);

        var (name, comment) = PlfTagNames.Read(b, names, afterNames, relations);
        if (b.D2(relations - 4) != RelationsMark)
        {
            throw new FwxFormatException(PlfFormat.Section, relations - 4, "нет маркера списка связей");
        }
        var count = b.D2(relations - 2);
        if (relations + count * 0x10L > end)
        {
            throw new FwxFormatException(PlfFormat.Section, relations, "связи выходят за пределы объекта");
        }
        var plcId = ReadPlcId(b, relations, count);
        var (symbolId, area, address) = ReadAccess(b, access, accessLimit);
        return new PlcTag(name, plcId, FindDataType(b, accessLimit, end), address, area, symbolId, comment);
    }

    /// <summary>Слот смещения в заголовке объекта: от начала объекта, в пределах [0x60, длина].</summary>
    /// <returns>Смещение от начала объекта.</returns>
    /// <exception cref="FwxFormatException">Смещение за пределами объекта.</exception>
    private static long Slot(FwxBinary b, PlfObject obj, int slot)
    {
        var value = b.D4(obj.Offset + slot);
        if (value < PlfFormat.MinSlot || value > obj.Length)
        {
            throw new FwxFormatException(PlfFormat.Section, obj.Offset + slot, $"слот смещения 0x{value:x} за пределами объекта");
        }
        return value;
    }

    /// <summary>Связи по 16 байт <c>[тип][класс цели][ID цели][0]</c>; ищется связь «тег → ПЛК».</summary>
    /// <returns>ID объекта ПЛК.</returns>
    /// <exception cref="FwxFormatException">Связи с ПЛК нет.</exception>
    private static long ReadPlcId(FwxBinary b, long relations, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var r = relations + i * 0x10L;
            if (b.D4(r) == PlfFormat.PlcRelation && b.D4(r + 4) == PlfFormat.PlcClass)
            {
                return b.D4(r + 8);
            }
        }
        throw new FwxFormatException(PlfFormat.Section, relations, "у тега нет связи с ПЛК");
    }

    /// <summary>
    /// Блок доступа (длина — в u32 перед ним, не больше слота конца):
    /// <c>[1][0][ID символа][01][u32][шесть u32][01][u32][строка][строка][u32]</c>. Шестой из
    /// шести u32 — область. Строки — семейство CPU и адрес в любом порядке: адрес начинается с
    /// <c>%</c>.
    /// </summary>
    /// <returns>ID символа, область и адрес.</returns>
    /// <exception cref="FwxFormatException">Блок не сходится с раскладкой или адрес не разрешён.</exception>
    private static (long SymbolId, PlcArea? Area, string Address) ReadAccess(FwxBinary b, long start, long limit)
    {
        var end = start + b.D4(start - 4);
        if (end > limit || b.D4(start) != 1 || b.D4(start + 4) != 0)
        {
            throw new FwxFormatException(PlfFormat.Section, start, "блок доступа не сходится с раскладкой");
        }
        if (b.D1(start + 0x0c) != ResolvedFlag || b.D1(start + 0x29) != ResolvedFlag)
        {
            throw new FwxFormatException(PlfFormat.Section, start, "адрес тега не разрешён");
        }
        var symbolId = b.D4(start + 8);
        var area = PlcAreaCodes.FromSymbolic(b.D4(start + 0x25));
        var (first, afterFirst) = PlfText.ReadString(b, start + 0x2e, end);
        var (second, afterSecond) = PlfText.ReadString(b, afterFirst, end);
        return afterSecond + 4 == end
            ? (symbolId, area, first.StartsWith('%') ? first : second)
            : throw new FwxFormatException(PlfFormat.Section, afterSecond, "блок доступа кончается не там, где указано");
    }

    /// <summary>
    /// Тип данных в пределах 0x80 байт от конца блока доступа. Форма 1:
    /// <c>[u32 0x39 + префикс][01][строка]</c>. Форма 2: <c>[u32 0x39][01][25 00 0c …]</c>, а
    /// строка — после <see cref="SecondTypeFormMark"/>.
    /// </summary>
    /// <returns>Имя типа (UDT — в кавычках, как в TIA) или <c>null</c>, если не найден.</returns>
    private static string? FindDataType(FwxBinary b, long from, long end)
    {
        var limit = Math.Min(from + TypeSearchWindow, end) - 8;
        for (var p = from; p < limit; p++)
        {
            if (b.D1(p + 4) != 1)
            {
                continue;
            }
            var length = b.D4(p);
            // случайные байты могут не быть varint — тогда это не блок типа, ищем дальше
            var at = p;
            if (PlfText.Attempt<(long Value, long Next)?>(() => PlfText.ReadVarint(b, at + 5)) is not { } varint)
            {
                continue;
            }
            var (prefix, text) = varint;
            if (prefix >= 2 && length == TypeBlockBase + prefix && text + prefix - 1 <= end)
            {
                return b.GetUtf8(text, (int)prefix - 1);
            }
            if (length != TypeBlockBase || !b.Span(p + 5, SecondTypeFormStart.Length).SequenceEqual(SecondTypeFormStart))
            {
                continue;
            }
            var mark = b.Span(p, Math.Min(SecondTypeFormWindow, end - p)).IndexOf(SecondTypeFormMark);
            return mark < 0 ? null : PlfText.ReadString(b, p + mark + SecondTypeFormMark.Length, end).Text;
        }
        return null;
    }
}
