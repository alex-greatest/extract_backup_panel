using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Лист «Классы алармов» книги <c>panel_data.xlsx</c>: колонка по заголовку и заливка колонок цвета фона.</summary>
public static class AlarmClassesSheetDump
{
    /// <summary>Имя листа классов алармов.</summary>
    public const string SheetName = "Классы алармов";

    /// <summary>Начало заголовка колонок цвета фона: <c>Background color "Incoming"</c> и три других.</summary>
    private const string ColorColumnPrefix = "Background color";

    /// <summary>
    /// Значения одной колонки по строкам классов, без заголовка. Колонка ищется по заголовку в первой строке листа, а
    /// значения читаются из её ячеек — текст с <c> | </c> в соседних ячейках колонки не сдвигает.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="header">Заголовок колонки целиком (<c>Log</c>, <c>E-mail address</c>).</param>
    /// <returns>Значения колонки в порядке строк.</returns>
    public static List<string> Column(string workbookPath, string header)
    {
        using var workbook = new XLWorkbook(workbookPath);
        var sheet = SheetDump.Sheet(workbook, SheetName);
        var cell = sheet.Row(1).CellsUsed().FirstOrDefault(c => c.GetString() == header);
        if (cell is null)
        {
            Assert.Fail($"на листе «{SheetName}» нет колонки «{header}»");
        }
        var column = cell.Address.ColumnNumber;
        return [.. sheet.RowsUsed().Skip(1).Select(r => r.Cell(column).GetString())];
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
        var sheet = SheetDump.Sheet(workbook, SheetName);
        var columns = sheet.Row(1).CellsUsed()
            .Where(c => c.GetString().StartsWith(ColorColumnPrefix, StringComparison.Ordinal))
            .Select(c => c.Address.ColumnNumber)
            .ToList();
        Assert.Equal(4, columns.Count);
        return [.. sheet.RowsUsed().Skip(1).Select(r => string.Join(" | ", [r.Cell(1).GetString(), .. columns.Select(c => Fill(r.Cell(c)))]))];
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
