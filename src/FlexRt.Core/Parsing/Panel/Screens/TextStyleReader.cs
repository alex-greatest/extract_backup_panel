using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Элемент PS_Text (32 байта): цвет, выравнивание и поля текста объекта. Сверено с TIA на шаговых копиях
/// <c>data/real/steps-iofield/</c> 16, 24–27 и 48 (Project1, V21); поля — ещё на 105 полях A603A0097 (Openness, по экранам).
/// </summary>
internal static class TextStyleReader
{
    /// <summary>Имя таблицы.</summary>
    public const string Table = "PS_Text";

    /// <summary>R, G, B цвета текста.</summary>
    private const int ColorOffset = 0x14;

    /// <summary>Байт выравнивания по горизонтали: 0 — Right, 1 — Center, 2 — Left.</summary>
    private const int HorizontalOffset = 0x18;

    /// <summary>Байт выравнивания по вертикали: 0 — Top, 1 — Middle, 2 — Bottom.</summary>
    private const int VerticalOffset = 0x19;

    /// <summary>Байты полей текста подряд: Top, Bottom, Left, Right (шаг 48: 6, 8, 5, 7).</summary>
    private const int MarginsOffset = 0x1c;

    /// <summary>Длина элемента с полями текста: до их конца.</summary>
    private const int MarginsEnd = MarginsOffset + 4;

    /// <summary>Наименьшая длина элемента: до конца байта выравнивания по вертикали; поля текста — только если помещаются.</summary>
    private const int MinLength = VerticalOffset + 1;

    /// <summary>
    /// Прочитать цвет, выравнивание и поля текста по ссылке из элемента объекта. Поля не помещаются в элемент — их нет и
    /// предупреждение с таблицей и смещением, одно на элемент (элемент стиля общий у многих объектов).
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица (Id из ссылки, имя — Table).</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения.</param>
    /// <returns>Стиль текста.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последним элементом, элемент вне таблицы или короче выравнивания.</exception>
    public static TextStyle Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, MinLength, owner);
        var b = doc.Binary;
        var p = item.Offset;
        return new TextStyle(
            StyleElement.Rgb(b, p + ColorOffset),
            b.D1(p + HorizontalOffset),
            b.D1(p + VerticalOffset),
            Margins(doc, table, item));
    }

    /// <summary>Поля текста: четыре байта с <see cref="MarginsOffset"/> — Top, Bottom, Left, Right.</summary>
    /// <returns>Поля; не помещаются в элемент — <c>null</c> и предупреждение (если такого ещё нет).</returns>
    private static TextMargins? Margins(FwxDocument doc, TocEntry table, TableItem item)
    {
        if (item.Length < MarginsEnd)
        {
            var warning = new FwxFormatException(table.Name, item.Offset,
                $"запись {item.Index + 1}: длина 0x{item.Length:x}, Margins не прочитаны").Message;
            if (!doc.Warnings.Contains(warning))
            {
                doc.Warnings.Add(warning);
            }
            return null;
        }
        var b = doc.Binary;
        var p = item.Offset + MarginsOffset;
        return new TextMargins(b.D1(p), b.D1(p + 1), b.D1(p + 2), b.D1(p + 3));
    }
}
