using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Лист «Дискретные алармы» книги <c>panel_data.xlsx</c>: заголовок и строки алармов.</summary>
public static class DiscreteAlarmsSheetDump
{
    /// <summary>Имя листа дискретных алармов.</summary>
    private const string SheetName = "Дискретные алармы";

    /// <summary>
    /// Прочитать лист: по строке на строку листа, ячейки через <c> | </c>
    /// (<c>1 | Аварийный стоп … | Errors | Alarms_Events | 0 | %DB1000.DBX1.0 | …</c>). Текст ячейки —
    /// как его показывает Excel; переносы строки внутри ячейки записываются как <c>\r</c> и
    /// <c>\n</c>, чтобы строка листа оставалась одной строкой эталона.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <returns>Строки листа по порядку, первая — заголовок.</returns>
    public static List<string> Read(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.True(workbook.TryGetWorksheet(SheetName, out var sheet), $"в книге нет листа «{SheetName}»");
        var lastColumn = sheet.LastColumnUsed()!.ColumnNumber();
        return [.. sheet.RowsUsed().Select(r => string.Join(" | ", Enumerable.Range(1, lastColumn).Select(c => Escape(r.Cell(c).GetString()))))];
    }

    /// <summary>Заменить символы переноса строки на <c>\r</c> и <c>\n</c>.</summary>
    /// <returns>Текст в одну строку.</returns>
    private static string Escape(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");

    /// <summary>Проверить, что в книге нет листа «Дискретные алармы».</summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    public static void Absent(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.False(workbook.TryGetWorksheet(SheetName, out _), $"в книге есть лист «{SheetName}»");
    }
}
