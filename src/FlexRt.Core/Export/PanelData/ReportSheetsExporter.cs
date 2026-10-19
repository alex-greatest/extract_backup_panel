using System.Globalization;
using ClosedXML.Excel;
using FlexRt.Export.PanelData.Tags;

namespace FlexRt.Export.PanelData;

/// <summary>
/// Книга Excel из готовых таблиц листов <c>panel_data.xlsx</c> — для окна, которое таблицы уже построило. Раскладка
/// листов та же, что у <see cref="PanelDataExporter"/>.
/// </summary>
public static class ReportSheetsExporter
{
    /// <summary>
    /// Записать книгу: по листу на <paramref name="sheets"/> в их порядке. «Сводка» — как <see cref="SummarySheet"/>, лист со
    /// статистикой — как <see cref="TagsSheet"/>, остальные — таблица с первой строки (<see cref="ReportTableWriter.Write"/>).
    /// </summary>
    /// <exception cref="ArgumentException">Листов нет: книгу без листов Excel не сохраняет.</exception>
    /// <exception cref="IOException">Файл не удалось записать (например, открыт в Excel).</exception>
    public static void Export(IReadOnlyList<ReportSheet> sheets, string xlsxPath)
    {
        if (sheets.Count == 0)
        {
            throw new ArgumentException("нет листов для записи", nameof(sheets));
        }
        using var workbook = new XLWorkbook();
        foreach (var sheet in sheets)
        {
            Write(workbook, sheet);
        }
        workbook.SaveAs(xlsxPath);
    }

    /// <summary>Текст ячейки для «Сводки», где подпись и значение — строки: число пишется текстом, как в консоли.</summary>
    /// <returns>Текст ячейки; пустая ячейка — пустая строка.</returns>
    private static string Text(ReportCell cell) => cell.Text ?? cell.Number?.ToString(CultureInfo.InvariantCulture) ?? "";

    /// <summary>Добавить в книгу один лист по его раскладке (<see cref="ReportSheet"/>).</summary>
    private static void Write(XLWorkbook workbook, ReportSheet sheet)
    {
        if (sheet.IsSummary)
        {
            SummarySheet.Write(workbook, [.. sheet.Table.Rows.Select(r => (Text(r[0]), Text(r[1])))]);
        }
        else if (sheet.Statistics.Count > 0)
        {
            TagsSheet.Write(workbook, sheet.Statistics, sheet.Table);
        }
        else
        {
            ReportTableWriter.Write(workbook.Worksheets.Add(sheet.Name), 1, sheet.Table);
        }
    }
}
