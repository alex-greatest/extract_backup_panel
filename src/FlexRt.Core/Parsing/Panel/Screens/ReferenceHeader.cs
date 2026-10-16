using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Начало элемента GUI и ADAPTVALUECHANG: u32 (наблюдение: всегда 1), u32 R — число ссылок, R ссылок по 8 байт.</summary>
internal static class ReferenceHeader
{
    /// <summary>Смещение u32 числа ссылок R.</summary>
    private const int ReferenceCountOffset = 4;

    /// <summary>Начало ссылок: за u32 и u32 числом ссылок.</summary>
    private const int ReferencesOffset = 8;

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Наибольшее число ссылок, которое принимается без ошибки (наблюдение: GUI — 3, ADAPTVALUECHANG — 0 или 1).</summary>
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
}
