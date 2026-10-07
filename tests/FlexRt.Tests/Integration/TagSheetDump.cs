using ClosedXML.Excel;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Текстовый слепок листа «Теги» в формате эталона <c>samples/expected/теги.txt</c>:
/// по строке на строку листа, ячейки <c>A2=Tag_ScreenNumber</c> через <c>" | "</c>,
/// пустые ячейки не пишутся.
/// </summary>
public static class TagSheetDump
{
    /// <summary>Имя листа тегов в <c>panel_data.xlsx</c>.</summary>
    private const string SheetName = "Теги";

    /// <summary>
    /// Читает лист «Теги» и возвращает его слепок.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <returns>Строки слепка, включая строку заголовка листа.</returns>
    public static List<string> Read(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        return [.. workbook.Worksheet(SheetName).RowsUsed().Select(DumpRow)];
    }

    /// <summary>
    /// Формирует строку слепка для одной строки листа.
    /// </summary>
    /// <param name="row">Строка листа.</param>
    /// <returns>Непустые ячейки вида <c>адрес=текст</c> через <c>" | "</c>.</returns>
    private static string DumpRow(IXLRow row) => string.Join(" | ", row.CellsUsed()
        .Where(c => c.GetString().Length > 0)
        .Select(c => $"{c.Address}={c.GetString()}"));
}
