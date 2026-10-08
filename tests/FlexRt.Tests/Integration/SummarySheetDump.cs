using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Лист «Сводка» книги <c>panel_data.xlsx</c> в виде строк <c>подпись=значение</c>.</summary>
public static class SummarySheetDump
{
    /// <summary>Имя листа сводки.</summary>
    private const string SheetName = "Сводка";

    /// <summary>
    /// Прочитать лист «Сводка»: по строке на непустую строку листа, <c>A=B</c>
    /// (<c>Модель панели=TP1500 Comfort V2</c>). Проверяет, что лист в книге первый.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <returns>Строки сводки по порядку.</returns>
    public static List<string> Read(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.Equal(SheetName, workbook.Worksheet(1).Name);
        return [.. workbook.Worksheet(SheetName).RowsUsed().Select(r => $"{r.Cell(1).GetString()}={r.Cell(2).GetString()}")];
    }

    /// <summary>Проверить, что в сводке есть все строки, каждая целиком.</summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="expected">Ожидаемые строки <c>подпись=значение</c>.</param>
    public static void Contains(string workbookPath, params string[] expected)
    {
        var rows = Read(workbookPath);
        var missing = expected.Where(e => !rows.Contains(e)).ToList();
        Assert.True(missing.Count == 0, $"в листе «Сводка» нет строк:\n  {string.Join("\n  ", missing)}\nесть:\n  {string.Join("\n  ", rows)}");
    }
}
