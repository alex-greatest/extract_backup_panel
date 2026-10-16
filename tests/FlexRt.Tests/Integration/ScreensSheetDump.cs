using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Лист «Экраны» книги <c>panel_data.xlsx</c>: заголовок и строки экранов.</summary>
public static class ScreensSheetDump
{
    /// <summary>Имя листа экранов.</summary>
    private const string SheetName = "Экраны";

    /// <summary>
    /// Прочитать лист: по строке на строку листа, ячейки через <c> | </c>
    /// (<c>Root screen | 1 | Yes | 800 | 420 | Screen</c>). Текст ячейки — как его показывает Excel.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <returns>Строки листа по порядку, первая — заголовок.</returns>
    public static List<string> Read(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.True(workbook.TryGetWorksheet(SheetName, out var sheet), $"в книге нет листа «{SheetName}»");
        var lastColumn = sheet.LastColumnUsed()!.ColumnNumber();
        return [.. sheet.RowsUsed().Select(r => string.Join(" | ", Enumerable.Range(1, lastColumn).Select(c => r.Cell(c).GetString())))];
    }
}
