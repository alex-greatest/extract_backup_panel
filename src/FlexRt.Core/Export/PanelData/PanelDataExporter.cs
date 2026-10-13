using ClosedXML.Excel;
using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;

namespace FlexRt.Export.PanelData;

/// <summary>Данные панели в Excel: лист «Сводка», лист «Теги», лист «Системные события», лист «Дискретные алармы», лист «Аналоговые алармы», лист «Классы алармов» и, если есть ошибки файла ПЛК, лист «Ошибки».</summary>
public static class PanelDataExporter
{
    /// <summary>
    /// Записать книгу: первым — лист «Сводка» (<see cref="SummarySheet.Write"/>), затем лист
    /// «Теги» (<see cref="TagsSheet.Write"/>), лист «Системные события»
    /// (<see cref="SystemEventsSheet.Write"/>), лист «Дискретные алармы»
    /// (<see cref="DiscreteAlarmsSheet.Write"/>), лист «Аналоговые алармы»
    /// (<see cref="AnalogAlarmsSheet.Write"/>) и лист «Классы алармов»
    /// (<see cref="AlarmClassesSheet.Write"/>) — если они есть; лист «Ошибки» — только если
    /// <paramref name="errors"/> не пуст, по строке на сообщение. Текст пишется как есть
    /// (<c>=...</c> не становится формулой).
    /// </summary>
    /// <returns>Число тегов.</returns>
    /// <exception cref="IOException">Файл не удалось записать (например, открыт в Excel).</exception>
    public static int Export(FwxDocument doc, PlcProject? project, RunSummary summary, IReadOnlyList<PlcMatch?> matches, IReadOnlyList<string> errors, string xlsxPath)
    {
        using var workbook = new XLWorkbook();
        SummarySheet.Write(workbook, doc, project, summary);
        TagsSheet.Write(workbook, doc, matches);
        if (doc.SystemEvents.Count > 0)
        {
            SystemEventsSheet.Write(workbook, doc);
        }
        if (doc.DiscreteAlarms.Count > 0)
        {
            DiscreteAlarmsSheet.Write(workbook, doc, matches);
        }
        if (doc.AnalogAlarms.Count > 0)
        {
            AnalogAlarmsSheet.Write(workbook, doc);
        }
        if (AlarmClassesTable.Classes(doc).Count > 0)
        {
            AlarmClassesSheet.Write(workbook, doc);
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
        var sheet = workbook.Worksheets.Add("Ошибки");
        sheet.Cell(1, 1).Value = "Сообщение";
        for (var i = 0; i < errors.Count; i++)
        {
            sheet.Cell(i + 2, 1).SetValue(ExcelCell.Text(errors[i]));
        }
    }
}
