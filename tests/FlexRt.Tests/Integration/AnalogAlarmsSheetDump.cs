using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Лист «Аналоговые алармы» книги <c>panel_data.xlsx</c>: заголовок и строки алармов.</summary>
public static class AnalogAlarmsSheetDump
{
    /// <summary>Имя листа аналоговых алармов.</summary>
    private const string SheetName = "Аналоговые алармы";

    /// <summary>
    /// Прочитать лист: по строке на строку листа, ячейки через <c> | </c>
    /// (<c>2 | ANA3 |  |  | Errors | DB_ST7_Analog_Channel_2_Value | 13 | Higher | On</c>). Текст
    /// ячейки — как его показывает Excel.
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

    /// <summary>Проверить, что в книге нет листа «Аналоговые алармы».</summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    public static void Absent(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.False(workbook.TryGetWorksheet(SheetName, out _), $"в книге есть лист «{SheetName}»");
    }
}
