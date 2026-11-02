using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>
/// Начало элемента GUI, ADAPTVALUECHANG, VIEW_DYN, LIMIT, FUNC_LIST, ADAPTDEMULTIPLE, ADAPTLOOPBREAKE и FUNC_*: u32 (наблюдение: 1,
/// у ADAPTDEMULTIPLE и ADAPTLOOPBREAKE — 0; здесь не проверяется), u32 R — число ссылок, R ссылок по 8 байт.
/// </summary>
internal static class ReferenceHeader
{
    /// <summary>Смещение u32 числа ссылок R.</summary>
    private const int ReferenceCountOffset = 4;

    /// <summary>Начало ссылок: за u32 и u32 числом ссылок.</summary>
    private const int ReferencesOffset = 8;

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Размер u32 числа ссылок R.</summary>
    private const int CountSize = 4;

    /// <summary>Наибольшее число ссылок, которое принимается без ошибки (наблюдение: GUI — 3, ADAPTVALUECHANG — 0 или 1, VIEW_DYN — 1, LIMIT у Visibility экрана — 1).</summary>
    private const long MaxReferences = 0x20;

    /// <summary>
    /// Прочитать начало элемента: число ссылок и ссылки — Id таблицы и номер элемента (два слова ссылки не читаются).
    /// Поле за ссылками начинается сразу после них.
    /// </summary>
    /// <returns>Смещение конца ссылок от начала элемента и ссылки в порядке файла.</returns>
    /// <exception cref="FwxFormatException">Элемент короче 8 байт, R больше 0x20 или ссылки не помещаются в элемент.</exception>
    public static (long End, IReadOnlyList<VarReference> References) Read(FwxBinary b, TableItem item) => Read(b, item, b.Section);

    /// <summary>
    /// Как <see cref="Read(FwxBinary, TableItem)"/>, но таблица для сообщения об ошибке задана явно, а не берётся из
    /// <see cref="FwxBinary.Section"/> (её меняет чтение элементов других таблиц по ссылкам).
    /// </summary>
    /// <param name="b">Байты файла.</param>
    /// <param name="item">Элемент таблицы.</param>
    /// <param name="section">Имя таблицы элемента — для сообщения.</param>
    /// <returns>Смещение конца ссылок от начала элемента и ссылки в порядке файла.</returns>
    /// <exception cref="FwxFormatException">Элемент короче 8 байт, R больше 0x20 или ссылки не помещаются в элемент.</exception>
    public static (long End, IReadOnlyList<VarReference> References) Read(FwxBinary b, TableItem item, string section)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < ReferencesOffset)
        {
            throw new FwxFormatException(section, p, $"запись {number}: длина {item.Length} меньше 0x{ReferencesOffset:x}");
        }
        var links = TryReadAt(b, item, ReferenceCountOffset)
            ?? throw new FwxFormatException(section, p, $"запись {number}: {b.D4(p + ReferenceCountOffset)} ссылок не помещаются в {item.Length} байт");
        return (ReferencesOffset + links.Count * ReferenceSize, links);
    }

    /// <summary>
    /// Прочитать u32 R на <paramref name="countOffset"/> от начала элемента и R ссылок сразу за ним: так же, как в
    /// <see cref="Read(FwxBinary, TableItem)"/>, но с любого места (у элемента с именем — за именем, <see cref="NamedElement"/>) и без исключения.
    /// </summary>
    /// <returns>Ссылки в порядке файла; R или ссылки не помещаются в элемент, R больше 0x20 — <c>null</c>.</returns>
    public static IReadOnlyList<VarReference>? TryReadAt(FwxBinary b, TableItem item, long countOffset)
    {
        if (TryEndAt(b, item, countOffset) is not { } end)
        {
            return null;
        }
        var first = countOffset + CountSize;
        return
        [
            .. Enumerable.Range(0, (int)((end - first) / ReferenceSize))
                .Select(i => item.Offset + first + i * ReferenceSize)
                .Select(o => new VarReference(b.D2(o), b.D2(o + 2)))
        ];
    }

    /// <summary>
    /// Конец ссылок, как в <see cref="TryReadAt"/>: u32 R на <paramref name="countOffset"/> от начала элемента и R ссылок
    /// за ним; сами ссылки не читаются.
    /// </summary>
    /// <returns>Смещение конца ссылок от начала элемента; R или ссылки не помещаются в элемент, R больше 0x20 — <c>null</c>.</returns>
    public static long? TryEndAt(FwxBinary b, TableItem item, long countOffset)
    {
        if (countOffset + CountSize > item.Length)
        {
            return null;
        }
        var references = b.D4(item.Offset + countOffset);
        var end = countOffset + CountSize + references * ReferenceSize;
        return references > MaxReferences || end > item.Length ? null : end;
    }

    /// <summary>
    /// Прочитать u16 на <paramref name="offset"/> от конца ссылок (<see cref="Read(FwxBinary, TableItem)"/>): номер стартового экрана GUI,
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
