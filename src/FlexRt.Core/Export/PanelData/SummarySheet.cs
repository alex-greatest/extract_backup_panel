using ClosedXML.Excel;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Сводка»: основные сведения о панели, соединениях и ПЛК — подпись в A, значение в B.</summary>
public static class SummarySheet
{
    /// <summary>Имя листа в книге.</summary>
    public const string Name = "Сводка";

    /// <summary>
    /// Добавить лист «Сводка» в книгу: по строке на строку <see cref="SummaryRows.Build"/>, пустая
    /// строка остаётся пустой; значение — через <see cref="ExcelCell.Text"/>. Ширина колонки A — по
    /// содержимому.
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc, PlcProject? project, RunSummary summary) =>
        Write(workbook, Name, SummaryRows.Table(doc, project, summary));

    /// <summary>
    /// Добавить лист <paramref name="name"/> из таблицы «Сводки» (<see cref="SummaryRows.Table"/>): подпись в A, значение
    /// в B (<see cref="ReportCell.Display"/>, через <see cref="ExcelCell.Text"/>), без заголовков; строка с пустой подписью
    /// остаётся пустой. Ширина колонки A — по содержимому.
    /// </summary>
    public static void Write(XLWorkbook workbook, string name, ReportTable table)
    {
        var sheet = workbook.Worksheets.Add(name);
        var r = 1;
        foreach (var cells in table.Rows)
        {
            var label = cells[0].Display;
            if (label.Length > 0)
            {
                sheet.Cell(r, 1).SetValue(label);
                sheet.Cell(r, 2).SetValue(ExcelCell.Text(cells[1].Display));
            }
            r++;
        }
        sheet.Column(1).AdjustToContents();
    }
}
