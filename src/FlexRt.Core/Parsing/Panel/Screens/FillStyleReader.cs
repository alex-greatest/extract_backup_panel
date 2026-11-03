using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Элемент PS_Fill_Color (24 байта): цвет фона объекта. Сверено с TIA на шаговой копии
/// <c>data/real/steps-iofield/15_back_color.fwc</c> (Project1, V21). Fill pattern в файл не попадает (шаг 22).
/// </summary>
internal static class FillStyleReader
{
    /// <summary>Имя таблицы.</summary>
    public const string Table = "PS_Fill_Color";

    /// <summary>R, G, B цвета фона.</summary>
    private const int ColorOffset = 0x14;

    /// <summary>Наименьшая длина элемента: до конца цвета.</summary>
    private const int MinLength = ColorOffset + 3;

    /// <summary>Прочитать цвет фона по ссылке из элемента объекта.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица (Id из ссылки, имя — Table).</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения.</param>
    /// <returns>Заливка.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последним элементом, элемент вне таблицы или короче.</exception>
    public static FillStyle Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, MinLength, owner);
        return new FillStyle(StyleElement.Rgb(doc.Binary, item.Offset + ColorOffset));
    }
}
