using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Начало элемента GUI, ADAPTVALUECHANG и VIEW_DYN: u32 (наблюдение: всегда 1), u32 R — число ссылок, R ссылок по 8 байт.</summary>
internal static class ReferenceHeader
{
    /// <summary>Смещение u32 числа ссылок R.</summary>
    private const int ReferenceCountOffset = 4;

    /// <summary>Начало ссылок: за u32 и u32 числом ссылок.</summary>
    private const int ReferencesOffset = 8;

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Наибольшее число ссылок, которое принимается без ошибки (наблюдение: GUI — 3, ADAPTVALUECHANG — 0 или 1, VIEW_DYN — 1).</summary>
    private const long MaxReferences = 0x20;

    /// <summary>
    /// Прочитать начало элемента: число ссылок и Id таблиц, на которые они ведут. Поле за ссылками
    /// начинается сразу после них.
    /// </summary>
    /// <returns>Смещение конца ссылок от начала элемента и Id таблиц ссылок в порядке файла.</returns>
    /// <exception cref="FwxFormatException">Элемент короче 8 байт, R больше 0x20 или ссылки не помещаются в элемент.</exception>
    public static (long End, IReadOnlyList<int> Tables) Read(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < ReferencesOffset)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 0x{ReferencesOffset:x}");
        }
        var references = b.D4(p + ReferenceCountOffset);
        var end = ReferencesOffset + references * ReferenceSize;
        if (references > MaxReferences || end > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: {references} ссылок не помещаются в {item.Length} байт");
        }
        var tables = Enumerable.Range(0, (int)references)
            .Select(i => b.D2(p + ReferencesOffset + i * ReferenceSize))
            .ToList();
        return (end, tables);
    }

    /// <summary>
    /// Прочитать u16 на <paramref name="offset"/> от конца ссылок (<see cref="Read"/>): номер стартового экрана GUI,
    /// высота Permanent area VIEW_DYN.
    /// </summary>
    /// <param name="b">Байты файла.</param>
    /// <param name="item">Элемент таблицы.</param>
    /// <param name="offset">Смещение поля от конца ссылок.</param>
    /// <param name="what">Что за поле — для сообщения об ошибке («номер стартового экрана»).</param>
    /// <returns>Значение u16.</returns>
    /// <exception cref="FwxFormatException">Начало элемента не сходится или поле не помещается в элемент.</exception>
    public static int ReadU16After(FwxBinary b, TableItem item, int offset, string what)
    {
        var (end, _) = Read(b, item);
        var field = end + offset;
        return field + 2 <= item.Length
            ? b.D2(item.Offset + field)
            : throw new FwxFormatException(b.Section, item.Offset, $"запись {item.Index + 1}: {what} не помещается в {item.Length} байт");
    }
}
