using System.Numerics;
using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Условия VIEW_DARK: LIMIT для Range, SYMB для Single bit; опыты V21, TP700 Comfort.</summary>
internal static class FieldConditionReader
{
    /// <summary>Длина тела LIMIT: флаги и два VARIANT по 16 байт.</summary>
    private const int LimitBodySize = 0x28;

    /// <summary>Длина тела SYMB Single bit: заголовок, маска, заголовок значений и два i16.</summary>
    private const int BitBodySize = 0x10;

    /// <summary>Бит активного состояния в байте флагов LIMIT на +1 от конца ссылок.</summary>
    private const int ActiveFlag = 0x08;

    /// <summary>Прочитать условие, сохранив поле при ошибке данных анимации.</summary>
    /// <returns>Условие; при сбое Active null и предупреждение.</returns>
    public static FieldCondition Read(FwxDocument doc, VarReference link, FieldConditionKind kind, long sourceOffset)
    {
        try
        {
            return ReadRecord(doc, link, kind, sourceOffset);
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add($"{e.Message} (условие анимации поля не прочитано)");
            return new FieldCondition(kind, null, null, null, null, null);
        }
    }

    /// <summary>Прочитать Range или Single bit; неизвестная раскладка не подменяется выключенным условием.</summary>
    /// <returns>Условие анимации.</returns>
    /// <exception cref="FwxFormatException">Неизвестная таблица, раскладка или повреждённый элемент.</exception>
    private static FieldCondition ReadRecord(FwxDocument doc, VarReference link, FieldConditionKind kind, long sourceOffset)
    {
        var table = AnimationLinkReader.Table(doc, link, "VIEW_DARK", sourceOffset);
        var item = ElementLink.Item(doc, table, link.Index);
        var b = doc.Binary;
        var (end, links) = ReferenceHeader.Read(b, item, table.Name);
        if (links.Count > 0)
        {
            var tagTable = AnimationLinkReader.Table(doc, links[0], table.Name, item.Offset + 0x08);
            if (tagTable.Name is "VAR" or "VARLIST")
            {
                ElementLink.Item(doc, tagTable, links[0].Index);
            }
        }
        var tag = IoFieldTagReader.Read(doc, links).Name;
        var p = item.Offset + end;
        return table.Name switch
        {
            "LIMIT" when end + LimitBodySize <= item.Length => new FieldCondition(kind, tag, null,
                Value(b, table, p + 0x08), Value(b, table, p + 0x18), (b.D1(p + 1) & ActiveFlag) != 0),
            "SYMB" when end + BitBodySize <= item.Length && b.D2(p) == 1 && b.D2(p + 2) == 4 => ReadBit(b, table, item, p, kind, tag),
            _ => throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: условие анимации поля не разобрано")
        };
    }

    /// <summary>Маска одного бита и состояние объекта при значении бита 1.</summary>
    /// <returns>Условие Single bit.</returns>
    /// <exception cref="FwxFormatException">Маска или размер значения не соответствуют Single bit.</exception>
    private static FieldCondition ReadBit(FwxBinary b, TocEntry table, TableItem item, long p, FieldConditionKind kind, string? tag)
    {
        var mask = (uint)b.D4(p + 4);
        return BitOperations.IsPow2(mask) && b.D1(p + 0x0a) == 2
            ? new FieldCondition(kind, tag, BitOperations.TrailingZeroCount(mask), null, null, b.D2(p + 0x0e) != 0)
            : throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: условие Single bit не разобрано");
    }

    /// <summary>Значение VARIANT условия: i16, i32 либо f64.</summary>
    /// <returns>Числовая граница диапазона.</returns>
    /// <exception cref="FwxFormatException">Тип значения не установлен.</exception>
    private static double Value(FwxBinary b, TocEntry table, long offset) => b.D2(offset) switch
    {
        2 => (short)b.D2(offset + 0x08),
        3 => (int)b.D4(offset + 0x08),
        5 => b.F64(offset + 0x08),
        var type => throw new FwxFormatException(table.Name, offset, $"тип границы анимации 0x{type:X} не разобран")
    };
}
