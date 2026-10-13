using ClosedXML.Excel;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Аналоговые алармы»: алармы панели с колонками как Analog alarms в TIA, без имени аларма.</summary>
public static class AnalogAlarmsSheet
{
    /// <summary>
    /// Добавить лист «Аналоговые алармы» в книгу: с первой строки — таблица
    /// <see cref="AnalogAlarmsTable.Build"/> (<see cref="ReportTableWriter.Write"/>: число —
    /// числом, пустой текст не пишется, <c>=...</c> не становится формулой).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc)
    {
        var sheet = workbook.Worksheets.Add("Аналоговые алармы");
        ReportTableWriter.Write(sheet, 1, AnalogAlarmsTable.Build(doc));
    }
}
