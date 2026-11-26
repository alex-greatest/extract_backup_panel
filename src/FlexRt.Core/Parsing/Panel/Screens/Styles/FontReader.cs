using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens.Styles;

/// <summary>
/// Элемент FONT: шрифты объекта по языкам Runtime (116 байт на одноязычных копиях). Сверено с TIA на шаговых копиях <c>data/real/steps-iofield/</c> 28–31
/// (Project1, V21): размер, Bold, Italic, Underline. Имя шрифта — UTF-16LE до нуля; на шаге 32 (Arial) в файле
/// осталось <c>Tahoma</c> — не выяснено.
/// </summary>
internal static class FontReader
{
    /// <summary>Имя таблицы.</summary>
    public const string Table = "FONT";

    /// <summary>u16 число языковых записей; трёхъязычный опыт TP700 V21, 2026-11-15.</summary>
    private const int CountOffset = 0x0a;

    /// <summary>Начало первой языковой записи: u32 длина данных, u32 LCID, затем данные шрифта.</summary>
    private const int RecordOffset = 0x0c;

    /// <summary>Размер заголовка языковой записи: длина данных и LCID.</summary>
    private const int RecordHeaderSize = 0x08;

    /// <summary>LCID первой записи; у следующих — то же смещение относительно сдвинутого начала.</summary>
    private const int LanguageOffset = 0x10;

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

    /// <summary>u32 зачёркивание: 1 — вкл.; переключение в редакторе TP700 V21, опыты 2026-11-15.</summary>
    private const int StrikeoutOffset = 0x30;

    /// <summary>Имя шрифта UTF-16LE до нулевого символа или конца элемента.</summary>
    private const int NameOffset = 0x34;

    /// <summary>Прочитать шрифты по ссылке из элемента объекта; неполный дополнительный хвост остаётся неразобранным, как прежде.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица (Id из ссылки, имя — Table).</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения.</param>
    /// <returns>Первый шрифт и полные дополнительные языковые записи в порядке FONT.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последним элементом, элемент вне таблицы или короче имени.</exception>
    public static IReadOnlyList<LanguageFont> Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, NameOffset, owner);
        var b = doc.Binary;
        var next = RecordOffset + RecordHeaderSize + b.D4(item.Offset + RecordOffset);
        var first = next >= NameOffset && next <= item.Length ? item with { Length = next } : item;
        var fonts = new List<LanguageFont> { new((int)b.D4(item.Offset + LanguageOffset), ReadFont(doc, first)) };
        if (next < NameOffset || next > item.Length)
        {
            return fonts;
        }
        for (var i = 1; i < b.D2(item.Offset + CountOffset); i++)
        {
            if (next > item.Length - RecordHeaderSize)
            {
                break;
            }
            var end = next + RecordHeaderSize + b.D4(item.Offset + next);
            if (end < next + NameOffset - RecordOffset || end > item.Length)
            {
                break;
            }
            var shift = next - RecordOffset;
            var record = new TableItem(item.Index, item.Offset + shift, end - shift);
            fonts.Add(new LanguageFont((int)b.D4(record.Offset + LanguageOffset), ReadFont(doc, record)));
            next = end;
        }
        return fonts;
    }

    /// <summary>Прочитать одну языковую запись; имя ограничено её концом.</summary>
    /// <returns>Шрифт языка.</returns>
    private static PanelFont ReadFont(FwxDocument doc, TableItem item)
    {
        var b = doc.Binary;
        var p = item.Offset;
        return new PanelFont(
            Name(doc, item),
            b.D4(p + HeightOffset) / HeightPerSize,
            b.D2(p + WeightOffset) >= BoldWeight,
            b.D4(p + ItalicOffset) != 0,
            b.D4(p + UnderlineOffset) != 0,
            b.D4(p + StrikeoutOffset) != 0);
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
