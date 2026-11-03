using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Элемент PS_Text (32 байта): цвет и выравнивание текста объекта. Сверено с TIA на шаговых копиях
/// <c>data/real/steps-iofield/</c> 16 и 24–27 (Project1, V21).
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

    /// <summary>Наименьшая длина элемента: до конца байта выравнивания по вертикали.</summary>
    private const int MinLength = VerticalOffset + 1;

    /// <summary>Прочитать цвет и выравнивание текста по ссылке из элемента объекта.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица (Id из ссылки, имя — Table).</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения.</param>
    /// <returns>Стиль текста.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последним элементом, элемент вне таблицы или короче.</exception>
    public static TextStyle Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, MinLength, owner);
        var b = doc.Binary;
        var p = item.Offset;
        return new TextStyle(StyleElement.Rgb(b, p + ColorOffset), b.D1(p + HorizontalOffset), b.D1(p + VerticalOffset));
    }
}
