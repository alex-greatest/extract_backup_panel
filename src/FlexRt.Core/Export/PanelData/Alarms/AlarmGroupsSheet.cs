using ClosedXML.Excel;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Лист «Группы алармов»: ID групп, назначенных алармам, и число алармов в каждой.</summary>
public static class AlarmGroupsSheet
{
    /// <summary>
    /// Добавить лист «Группы алармов» в книгу: с первой строки — таблица
    /// <see cref="AlarmGroupsTable.Build"/> (<see cref="ReportTableWriter.Write"/>: числа — числами).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc)
    {
        var sheet = workbook.Worksheets.Add("Группы алармов");
        ReportTableWriter.Write(sheet, 1, AlarmGroupsTable.Build(doc));
    }
}
