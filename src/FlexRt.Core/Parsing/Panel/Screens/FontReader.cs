using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Элемент FONT (116 байт): шрифт объекта. Сверено с TIA на шаговых копиях <c>data/real/steps-iofield/</c> 28–31
/// (Project1, V21): размер, Bold, Italic, Underline. Имя шрифта — UTF-16LE до нуля; на шаге 32 (Arial) в файле
/// осталось <c>Tahoma</c> — не выяснено.
/// </summary>
internal static class FontReader
{
    /// <summary>Имя таблицы.</summary>
    public const string Table = "FONT";

    /// <summary>u32 высота: размер TIA × 7500 (16 → 120000, 20 → 150000).</summary>
    private const int HeightOffset = 0x1c;

    /// <summary>Высота на единицу размера TIA (наблюдение на двух размерах).</summary>
    private const double HeightPerSize = 7500;

    /// <summary>u16 вес: 400 — Regular, 700 — Bold.</summary>
    private const int WeightOffset = 0x24;

    /// <summary>Вес, начиная с которого шрифт жирный (как FW_BOLD Win32).</summary>
    private const int BoldWeight = 700;

    /// <summary>u32 курсив: 1 — вкл.</summary>
    private const int ItalicOffset = 0x28;

    /// <summary>u32 подчёркивание: 1 — вкл.</summary>
    private const int UnderlineOffset = 0x2c;

    /// <summary>Имя шрифта UTF-16LE до нулевого символа или конца элемента.</summary>
    private const int NameOffset = 0x34;

    /// <summary>Прочитать шрифт по ссылке из элемента объекта.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица (Id из ссылки, имя — Table).</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения.</param>
    /// <returns>Шрифт.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последним элементом, элемент вне таблицы или короче имени.</exception>
    public static PanelFont Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, NameOffset, owner);
        var b = doc.Binary;
        var p = item.Offset;
        return new PanelFont(
            Name(doc, item),
            b.D4(p + HeightOffset) / HeightPerSize,
            b.D2(p + WeightOffset) >= BoldWeight,
            b.D4(p + ItalicOffset) != 0,
            b.D4(p + UnderlineOffset) != 0);
    }

    /// <summary>Имя шрифта: символы UTF-16LE с <see cref="NameOffset"/> до нуля или конца элемента.</summary>
    /// <returns>Имя; пустое — пустая строка.</returns>
    private static string Name(FwxDocument doc, TableItem item)
    {
        var start = item.Offset + NameOffset;
        var chars = 0;
        while (NameOffset + (chars + 1) * 2L <= item.Length && doc.Binary.D2(start + chars * 2L) != 0)
        {
            chars++;
        }
        return doc.Binary.GetNameLen(start, chars);
    }
}
