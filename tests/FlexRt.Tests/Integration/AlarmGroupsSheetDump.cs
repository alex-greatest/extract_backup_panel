using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Лист «Группы алармов» книги <c>panel_data.xlsx</c>: заголовок и строки групп.</summary>
public static class AlarmGroupsSheetDump
{
    /// <summary>Имя листа групп алармов.</summary>
    private const string SheetName = "Группы алармов";

    /// <summary>
    /// Прочитать лист: по строке на строку листа, ячейки через <c> | </c> (<c>17 | 1 | 0</c>).
    /// Текст ячейки — как его показывает Excel.
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

    /// <summary>Проверить, что в книге нет листа «Группы алармов».</summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    public static void Absent(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.False(workbook.TryGetWorksheet(SheetName, out _), $"в книге есть лист «{SheetName}»");
    }
}
