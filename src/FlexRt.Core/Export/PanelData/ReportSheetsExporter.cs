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
    /// Записать книгу: по листу на <paramref name="sheets"/> в их порядке. «Сводка» — как <see cref="SummarySheet"/>, «Теги» — как
    /// <see cref="TagsSheet"/>, остальные — таблица с первой строки (<see cref="ReportTableWriter.Write"/>).
    /// </summary>
    /// <returns>Число записанных листов.</returns>
    /// <exception cref="ArgumentException">Листов нет: книгу без листов Excel не сохраняет.</exception>
    /// <exception cref="IOException">Файл не удалось записать (например, открыт в Excel).</exception>
    public static int Export(IReadOnlyList<ReportSheet> sheets, string xlsxPath)
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
        return sheets.Count;
    }

    /// <summary>Добавить в книгу один лист по его раскладке.</summary>
    private static void Write(XLWorkbook workbook, ReportSheet sheet)
    {
        switch (sheet.Kind)
        {
            case ReportSheetKind.Summary:
                SummarySheet.Write(workbook, [.. sheet.Table.Rows.Select(r => (r[0].Text ?? "", r[1].Text ?? ""))]);
                break;
            case ReportSheetKind.Tags:
                TagsSheet.Write(workbook, sheet.Statistics, sheet.Table);
                break;
            default:
                ReportTableWriter.Write(workbook.Worksheets.Add(sheet.Name), 1, sheet.Table);
                break;
        }
    }
}
