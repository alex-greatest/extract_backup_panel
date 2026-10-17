using System.Globalization;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Адрес бита тега дискретного аларма, как Trigger address и HMI acknowledgment address в TIA.</summary>
public static class AlarmBitAddress
{
    /// <summary>Текст ячейки, если данных нет: нет данных ПЛК, ПЛК не выбран или связь не разобрана.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>Текст ячейки, если ПЛК выбран, но тега в нём нет — как на листе «Теги».</summary>
    private const string NotInPlcFile = "отсутствует в файле ПЛК";

    /// <summary>
    /// Бит в элементе: TIA даёт выбрать тегом аларма только 16-битный тег (Int, UInt, Word) или
    /// массив таких — DWord и Byte в списке не появляются (проверено в TIA V21).
    /// </summary>
    private const int ElementBits = 16;

    /// <summary>
    /// Адрес бита <paramref name="bit"/> тега. Абсолютный доступ: бит считается от начала тега по
    /// словам, в слове старший байт идёт первым — бит 0..7 в байте +1, бит 8..15 в байте +0:
    /// <c>%DB80.DBW0</c> бит 0 → <c>%DB80.DBX1.0</c>, бит 8 → <c>%DB80.DBX0.0</c>, массив
    /// <c>%DB80.DBX60.0</c> бит 20 → <c>%DB80.DBX63.4</c>; у I/Q/M так же — <c>%MW10</c> бит 9 → <c>%M10.1</c>
    /// (сверено на Project1), I/Q не проверены.
    /// Символьный доступ: имя тега ПЛК и <c>.x</c> с битом, у массива — номер элемента и бит в нём:
    /// <c>DB.Member.x7</c>, <c>Data_block_1.11[1].x4</c>. Сверено с TIA на A603A0097: 12 адресов
    /// (Int, массив Word абсолютный и массив Int символьный). Внутренний тег — номер бита, как в
    /// TIA (сверено на Project1, V21: бит 0 → <c>0</c>, бит 5 → <c>5</c>, квитирование HMI бит 4 →
    /// <c>4</c>); тег не найден в ПЛК —
    /// «отсутствует в файле ПЛК»; нет данных — «неизвестно».
    /// </summary>
    /// <returns>Адрес.</returns>
    public static string Format(HmiTag tag, PlcMatch? match, int bit)
    {
        if (tag.LinkTable is null)
        {
            return bit.ToString(CultureInfo.InvariantCulture);
        }
        if (match?.Link is not { } link)
        {
            return Unknown;
        }
        return link.Absolute ? Absolute(link, bit) : Symbolic(tag, match, bit);
    }

    /// <summary>Адрес бита при абсолютном доступе: область, номер DB, байт и бит.</summary>
    /// <returns>Адрес или «неизвестно» у таймера, счётчика и незнакомой области.</returns>
    private static string Absolute(PlcLink link, int bit)
    {
        var prefix = link.Area switch
        {
            PlcArea.Input => "%I",
            PlcArea.Output => "%Q",
            PlcArea.Memory => "%M",
            PlcArea.DataBlock => $"%DB{link.DbNumber}.DBX",
            _ => null
        };
        if (prefix is null)
        {
            return Unknown;
        }
        var inWord = bit % ElementBits;
        var lowByte = inWord < 8 ? 1 : 0;
        var byteOffset = link.ByteOffset + 2L * (bit / ElementBits) + lowByte;
        return $"{prefix}{byteOffset}.{inWord % 8}";
    }

    /// <summary>Адрес бита при символьном доступе: имя тега ПЛК, у массива — с номером элемента.</summary>
    /// <returns>Адрес, «отсутствует в файле ПЛК» или «неизвестно».</returns>
    private static string Symbolic(HmiTag tag, PlcMatch match, int bit)
    {
        if (match.Lookup == PlcLookup.NotInPlcFile)
        {
            return NotInPlcFile;
        }
        if (match.Lookup != PlcLookup.Found || match.Tag?.Name is not { } path)
        {
            return Unknown;
        }
        return (tag.TypeCode & HmiTag.ArrayFlag) != 0
            ? $"{path}[{bit / ElementBits}].x{bit % ElementBits}"
            : $"{path}.x{bit}";
    }
}
