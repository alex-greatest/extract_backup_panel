using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>
/// Начало элемента списка по классам алармов, общее для RECMSG (классы журнала) и MSG_LOGGER
/// (функции классов): ссылки и число записей списка.
/// </summary>
internal static class ClassListHeader
{
    /// <summary>Начало ссылок: за u32 (наблюдение: всегда 1) и u32 числом ссылок.</summary>
    private const int ReferencesOffset = 8;

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Смещение u32 числа ссылок R.</summary>
    private const int ReferenceCountOffset = 4;

    /// <summary>Размер не разобранного u16 между ссылками и числом записей K.</summary>
    private const int UnknownWordSize = 2;

    /// <summary>Размер u16 числа записей K.</summary>
    private const int CountSize = 2;

    /// <summary>Наибольшее число ссылок, которое принимается без ошибки (наблюдение: 1).</summary>
    private const long MaxReferences = 0x20;

    /// <summary>
    /// Начало элемента (сверено на шаговых копиях A603A0097, V21): <c>+0x00</c> u32 (всегда 1, не
    /// разобрано), <c>+0x04</c> u32 R — число ссылок, R ссылок по 8 байт (наблюдение: одна, на
    /// MSG_HNDQUEUE), затем u16 (не разобрано: в RECMSG всегда 2, в MSG_LOGGER всегда 1) и u16 K —
    /// число записей списка. Записи идут сразу за K.
    /// </summary>
    /// <returns>Смещение первой записи от начала элемента и число записей K.</returns>
    /// <exception cref="FwxFormatException">Элемент короче 8 байт или ссылки и K не помещаются в элемент.</exception>
    public static (long EntriesOffset, int Count) Read(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < ReferencesOffset)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 0x{ReferencesOffset:x}");
        }
        var references = b.D4(p + ReferenceCountOffset);
        var countOffset = ReferencesOffset + references * ReferenceSize + UnknownWordSize;
        if (references > MaxReferences || countOffset + CountSize > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: {references} ссылок не помещаются в {item.Length} байт");
        }
        return (countOffset + CountSize, b.D2(p + countOffset));
    }
}
