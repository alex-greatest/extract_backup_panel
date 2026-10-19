using ClosedXML.Excel;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Сводка»: основные сведения о панели, соединениях и ПЛК — подпись в A, значение в B.</summary>
public static class SummarySheet
{
    /// <summary>
    /// Добавить лист «Сводка» в книгу: по строке на строку <see cref="SummaryRows.Build"/>, пустая
    /// строка остаётся пустой; значение — через <see cref="ExcelCell.Text"/>. Ширина колонки A — по
    /// содержимому.
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc, PlcProject? project, RunSummary summary) =>
        Write(workbook, SummaryRows.Build(doc, project, summary));

    /// <summary>
    /// Добавить лист «Сводка» из готовых строк (<see cref="SummaryRows.Build"/>): строка с пустой подписью остаётся
    /// пустой; значение — через <see cref="ExcelCell.Text"/>. Ширина колонки A — по содержимому.
    /// </summary>
    public static void Write(XLWorkbook workbook, IReadOnlyList<(string Label, string Value)> rows)
    {
        var sheet = workbook.Worksheets.Add("Сводка");
        var r = 1;
        foreach (var (label, value) in rows)
        {
            if (label.Length > 0)
            {
                sheet.Cell(r, 1).SetValue(label);
                sheet.Cell(r, 2).SetValue(ExcelCell.Text(value));
            }
            r++;
        }
        sheet.Column(1).AdjustToContents();
    }
}
