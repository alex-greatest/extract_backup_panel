using ClosedXML.Excel;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;

namespace FlexRt.Export.PanelData;

/// <summary>Данные панели в Excel у консоли: листы <see cref="PanelDataBook"/> и, если есть ошибки файла ПЛК, лист «Ошибки».</summary>
public static class PanelDataExporter
{
    /// <summary>Имя файла книги данных панели — у консоли в каталоге результата и по умолчанию в окне.</summary>
    public const string FileName = "panel_data.xlsx";

    /// <summary>Имя листа ошибок данных ПЛК.</summary>
    private const string ErrorsSheet = "Ошибки";

    /// <summary>
    /// Записать книгу: листы <see cref="PanelDataBook.Build"/> по порядку (<see cref="ReportSheetsExporter.Write"/>) и
    /// последним лист «Ошибки» — только если <paramref name="errors"/> не пуст, по строке на сообщение. Текст пишется как
    /// есть (<c>=...</c> не становится формулой).
    /// </summary>
    /// <returns>Число тегов.</returns>
    /// <exception cref="IOException">Файл не удалось записать (например, открыт в Excel).</exception>
    public static int Export(FwxDocument doc, PlcProject? project, RunSummary summary, IReadOnlyList<PlcMatch?> matches, IReadOnlyList<string> errors, string xlsxPath)
    {
        using var workbook = new XLWorkbook();
        var source = new PanelDataSource(doc, project, summary, new Lazy<IReadOnlyList<PlcMatch?>>(() => matches));
        foreach (var sheet in PanelDataBook.Build(source))
        {
            ReportSheetsExporter.Write(workbook, sheet);
        }
        if (errors.Count > 0)
        {
            WriteErrors(workbook, errors);
        }
        workbook.SaveAs(xlsxPath);
        return doc.Tags.Count;
    }

    /// <summary>Лист «Ошибки»: заголовок «Сообщение» и по строке на сообщение.</summary>
    private static void WriteErrors(XLWorkbook workbook, IReadOnlyList<string> errors)
    {
        var sheet = workbook.Worksheets.Add(ErrorsSheet);
        sheet.Cell(1, 1).Value = "Сообщение";
        for (var i = 0; i < errors.Count; i++)
        {
            sheet.Cell(i + 2, 1).SetValue(ExcelCell.Text(errors[i]));
        }
    }
}
