using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>
/// Начало элемента с именем, как у записи VAR: u16 вид, u16 длина имени в символах, имя UTF-16LE, нули до кратности 4,
/// u32 R, R ссылок по 8 байт (u16 Id таблицы, u16 номер элемента, два слова). Так начинаются объекты экрана
/// (VIEW_OCX, QtContainer), соединения (CONNECTION_S7) и элементы LIMIT с именем <c>тег~N</c> (наблюдение на копиях
/// <c>data/real/steps-events/</c> 20–23 и A603A0097).
/// </summary>
internal static class NamedElement
{
    /// <summary>Смещение u16 длины имени в символах: за u16 видом.</summary>
    private const int NameLengthOffset = 2;

    /// <summary>Смещение имени: за u16 видом и u16 длиной.</summary>
    private const int NameOffset = 4;

    /// <summary>Имя дополняется нулями до кратности 4 байтам (как у записи VAR), за ним — u32 R.</summary>
    private const int NameAlignment = 4;

    /// <summary>Вид элемента — первое слово (3 и 0x0b — объект с именем; у объектов без имени 1 и 9, длина имени 0).</summary>
    /// <returns>Слово; элемент короче 2 байт — <c>null</c>.</returns>
    public static int? Kind(FwxBinary b, TableItem item) => item.Length >= NameLengthOffset ? b.D2(item.Offset) : null;

    /// <summary>Имя элемента.</summary>
    /// <returns>Имя (у объекта без имени — пустое); длина или имя не помещаются в элемент — <c>null</c>.</returns>
    public static string? TryName(FwxBinary b, TableItem item)
    {
        if (item.Length < NameOffset)
        {
            return null;
        }
        var chars = b.D2(item.Offset + NameLengthOffset);
        return NameOffset + chars * 2L <= item.Length ? b.GetNameLen(item.Offset + NameOffset, chars) : null;
    }

    /// <summary>Ссылки элемента за именем (<see cref="ReferenceHeader.TryReadAt"/> с конца имени).</summary>
    /// <returns>
    /// Ссылки (Id таблицы и номер элемента) в порядке файла; имя, R или ссылки не помещаются в элемент, R больше 0x20 —
    /// <c>null</c>.
    /// </returns>
    public static IReadOnlyList<VarReference>? TryReferences(FwxBinary b, TableItem item) =>
        item.Length < NameOffset
            ? null
            : ReferenceHeader.TryReadAt(b, item, NameOffset + AlignedNameBytes(b.D2(item.Offset + NameLengthOffset)));

    /// <summary>Смещение поля за ссылками элемента (<see cref="TryReferences"/>) от начала элемента.</summary>
    /// <returns>Смещение конца ссылок; имя, R или ссылки не помещаются в элемент, R больше 0x20 — <c>null</c>.</returns>
    public static long? TryBodyOffset(FwxBinary b, TableItem item) =>
        item.Length < NameOffset
            ? null
            : ReferenceHeader.TryEndAt(b, item, NameOffset + AlignedNameBytes(b.D2(item.Offset + NameLengthOffset)));

    /// <summary>Байты имени с нулями до кратности <see cref="NameAlignment"/>.</summary>
    /// <returns>Длина имени в байтах, округлённая вверх до кратности 4.</returns>
    private static long AlignedNameBytes(int chars) => (chars * 2L + NameAlignment - 1) / NameAlignment * NameAlignment;
}
