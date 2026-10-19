using ClosedXML.Excel;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Screens;

/// <summary>Лист «Экраны»: экраны панели с номером, признаком стартового, размером и видом.</summary>
public static class ScreensSheet
{
    /// <summary>Имя листа в книге.</summary>
    public const string Name = "Экраны";

    /// <summary>
    /// Добавить лист «Экраны» в книгу: с первой строки — таблица <see cref="ScreensTable.Build"/>
    /// (<see cref="ReportTableWriter.Write"/>: числа — числами).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc)
    {
        var sheet = workbook.Worksheets.Add(Name);
        ReportTableWriter.Write(sheet, 1, ScreensTable.Build(doc));
    }
}
