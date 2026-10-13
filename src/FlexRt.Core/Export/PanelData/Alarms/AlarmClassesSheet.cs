using ClosedXML.Excel;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Лист «Классы алармов»: классы панели с колонками как Alarm classes в TIA.</summary>
public static class AlarmClassesSheet
{
    /// <summary>
    /// Добавить лист «Классы алармов» в книгу: с первой строки — таблица
    /// <see cref="AlarmClassesTable.Build"/> (<see cref="ReportTableWriter.Write"/>: число —
    /// числом, пустой текст не пишется, цвет фона — заливкой ячейки).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc)
    {
        var sheet = workbook.Worksheets.Add("Классы алармов");
        ReportTableWriter.Write(sheet, 1, AlarmClassesTable.Build(doc));
    }
}
