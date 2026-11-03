using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Элемент PS_BorderedShap (64 байта): рамка и углы объекта. Сверено с TIA на шаговых копиях
/// <c>data/real/steps-iofield/</c> 17–21 (Project1, V21): каждое поле меняется одно со своим свойством.
/// </summary>
internal static class BorderStyleReader
{
    /// <summary>Имя таблицы.</summary>
    public const string Table = "PS_BorderedShap";

    /// <summary>u16 стиль рамки: 1 — Solid, 2 — Double line.</summary>
    private const int EdgeStyleOffset = 0x14;

    /// <summary>u16 ширина рамки.</summary>
    private const int WidthOffset = 0x16;

    /// <summary>R, G, B цвета рамки.</summary>
    private const int ColorOffset = 0x18;

    /// <summary>R, G, B цвета фона рамки.</summary>
    private const int BackColorOffset = 0x1c;

    /// <summary>Четыре u16 радиуса углов (наблюдение: всегда равны, TIA задаёт один); читается первый.</summary>
    private const int CornerRadiusOffset = 0x20;

    /// <summary>Наименьшая длина элемента: до конца первого радиуса.</summary>
    private const int MinLength = CornerRadiusOffset + 2;

    /// <summary>Прочитать рамку по ссылке из элемента объекта.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица (Id из ссылки, имя — Table).</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения.</param>
    /// <returns>Рамка и углы.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последним элементом, элемент вне таблицы или короче.</exception>
    public static BorderStyle Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, MinLength, owner);
        var b = doc.Binary;
        var p = item.Offset;
        return new BorderStyle(
            b.D2(p + EdgeStyleOffset),
            b.D2(p + WidthOffset),
            StyleElement.Rgb(b, p + ColorOffset),
            StyleElement.Rgb(b, p + BackColorOffset),
            b.D2(p + CornerRadiusOffset));
    }
}
