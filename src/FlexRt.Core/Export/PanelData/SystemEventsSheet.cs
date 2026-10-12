using ClosedXML.Excel;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Системные события»: номер события и его текст на каждом языке панели, как System events в TIA.</summary>
public static class SystemEventsSheet
{
    /// <summary>
    /// Добавить лист «Системные события» в книгу: с первой строки — таблица
    /// <see cref="SystemEventsTable.Build"/> (<see cref="ReportTableWriter.Write"/>: пустой текст не
    /// пишется, <c>=...</c> не становится формулой).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc)
    {
        var sheet = workbook.Worksheets.Add("Системные события");
        ReportTableWriter.Write(sheet, 1, SystemEventsTable.Build(doc));
    }
}
