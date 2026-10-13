using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Лист «Классы алармов» книги <c>panel_data.xlsx</c>: заголовок, строки классов и заливка колонок цвета фона.</summary>
public static class AlarmClassesSheetDump
{
    /// <summary>Имя листа классов алармов.</summary>
    private const string SheetName = "Классы алармов";

    /// <summary>Начало заголовка колонок цвета фона: <c>Background color "Incoming"</c> и три других.</summary>
    private const string ColorColumnPrefix = "Background color";

    /// <summary>
    /// Прочитать лист: по строке на строку листа, ячейки через <c> | </c>
    /// (<c>64 | DISP | … | 255, 0, 0 | 0, 0, 255 | …</c>). Текст ячейки — как его показывает Excel.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <returns>Строки листа по порядку, первая — заголовок.</returns>
    public static List<string> Read(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        var sheet = Sheet(workbook);
        var lastColumn = sheet.LastColumnUsed()!.ColumnNumber();
        return [.. sheet.RowsUsed().Select(r => string.Join(" | ", Enumerable.Range(1, lastColumn).Select(c => r.Cell(c).GetString())))];
    }

    /// <summary>
    /// Значения одной колонки по строкам классов, без заголовка.
    /// </summary>
    /// <param name="rows">Строки листа из <see cref="Read"/>, первая — заголовок.</param>
    /// <param name="header">Заголовок колонки целиком (<c>Log</c>, <c>E-mail address</c>).</param>
    /// <returns>Значения колонки в порядке строк.</returns>
    public static List<string> Column(IReadOnlyList<string> rows, string header)
    {
        var index = Array.IndexOf(rows[0].Split(" | "), header);
        Assert.True(index >= 0, $"на листе «{SheetName}» нет колонки «{header}»: {rows[0]}");
        return [.. rows.Skip(1).Select(r => r.Split(" | ")[index])];
    }

    /// <summary>
    /// Прочитать заливку колонок цвета фона: по строке на класс, <c>ID | заливка | заливка | заливка |
    /// заливка</c>. Заливка — <c>RRGGBB</c>, с суффиксом <c> белый</c>, если текст ячейки белый; без
    /// заливки — <c>нет</c>.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <returns>Строки классов по порядку листа.</returns>
    public static List<string> Fills(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        var sheet = Sheet(workbook);
        var columns = sheet.Row(1).CellsUsed()
            .Where(c => c.GetString().StartsWith(ColorColumnPrefix, StringComparison.Ordinal))
            .Select(c => c.Address.ColumnNumber)
            .ToList();
        Assert.Equal(4, columns.Count);
        return [.. sheet.RowsUsed().Skip(1).Select(r => string.Join(" | ", [r.Cell(1).GetString(), .. columns.Select(c => Fill(r.Cell(c)))]))];
    }

    /// <summary>Лист «Классы алармов»; его отсутствие — провал теста.</summary>
    /// <returns>Лист книги.</returns>
    private static IXLWorksheet Sheet(XLWorkbook workbook)
    {
        Assert.True(workbook.TryGetWorksheet(SheetName, out var sheet), $"в книге нет листа «{SheetName}»");
        return sheet;
    }

    /// <summary>Заливка ячейки текстом: <c>RRGGBB</c>, <c>RRGGBB белый</c> или <c>нет</c>.</summary>
    /// <returns>Описание заливки и цвета текста.</returns>
    private static string Fill(IXLCell cell)
    {
        var style = cell.Style;
        if (style.Fill.PatternType == XLFillPatternValues.None)
        {
            return "нет";
        }
        var fill = style.Fill.BackgroundColor.Color;
        var font = style.Font.FontColor;
        var white = font.ColorType == XLColorType.Color && font.Color is { R: 255, G: 255, B: 255 };
        return $"{fill.R:X2}{fill.G:X2}{fill.B:X2}{(white ? " белый" : "")}";
    }
}
