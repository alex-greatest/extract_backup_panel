using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Слепок листа-таблицы <c>panel_data.xlsx</c> («Системные события», «Дискретные алармы», «Аналоговые алармы», «Классы
/// алармов», «Группы алармов», «Экраны»): по строке на строку листа с заголовком, ячейки с первой по последнюю
/// использованную колонку через <c> | </c>; переносы строки в тексте ячейки — <c>\r</c> и <c>\n</c> буквально, чтобы строка
/// листа была одной строкой эталона.
/// </summary>
public static class SheetDump
{
    /// <summary>Прочитать слепок листа.</summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="sheetName">Имя листа.</param>
    /// <returns>Строки слепка, первая — заголовок.</returns>
    public static List<string> Read(string workbookPath, string sheetName)
    {
        using var workbook = new XLWorkbook(workbookPath);
        var sheet = Sheet(workbook, sheetName);
        var lastColumn = sheet.LastColumnUsed()!.ColumnNumber();
        return [.. sheet.RowsUsed().Select(r => string.Join(" | ", Enumerable.Range(1, lastColumn).Select(c => Escape(r.Cell(c).GetString()))))];
    }

    /// <summary>Проверить, что листа в книге нет.</summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="sheetName">Имя листа.</param>
    public static void Absent(string workbookPath, string sheetName)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.False(workbook.TryGetWorksheet(sheetName, out _), $"в книге есть лист «{sheetName}»");
    }

    /// <summary>Лист книги; его нет — провал теста.</summary>
    /// <param name="workbook">Открытая книга.</param>
    /// <param name="sheetName">Имя листа.</param>
    /// <returns>Лист.</returns>
    public static IXLWorksheet Sheet(XLWorkbook workbook, string sheetName)
    {
        Assert.True(workbook.TryGetWorksheet(sheetName, out var sheet), $"в книге нет листа «{sheetName}»");
        return sheet;
    }

    /// <summary>Переносы строки в тексте ячейки — буквами <c>\r</c> и <c>\n</c>.</summary>
    /// <returns>Текст в одну строку.</returns>
    private static string Escape(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");
}
