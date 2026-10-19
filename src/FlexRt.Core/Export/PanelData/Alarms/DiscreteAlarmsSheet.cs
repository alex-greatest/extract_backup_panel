using ClosedXML.Excel;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Лист «Дискретные алармы»: алармы панели с колонками как Discrete alarms в TIA, без имени аларма.</summary>
public static class DiscreteAlarmsSheet
{
    /// <summary>Имя листа в книге.</summary>
    public const string Name = "Дискретные алармы";

    /// <summary>
    /// Добавить лист «Дискретные алармы» в книгу: с первой строки — таблица
    /// <see cref="DiscreteAlarmsTable.Build"/> (<see cref="ReportTableWriter.Write"/>: число —
    /// числом, пустой текст не пишется, <c>=...</c> не становится формулой).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc, IReadOnlyList<PlcMatch?> matches)
    {
        var sheet = workbook.Worksheets.Add(Name);
        ReportTableWriter.Write(sheet, 1, DiscreteAlarmsTable.Build(doc, matches));
    }
}
