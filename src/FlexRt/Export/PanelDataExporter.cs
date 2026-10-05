using ClosedXML.Excel;
using FlexRt.Model;

namespace FlexRt.Export;

/// <summary>Данные панели в Excel: лист «Теги».</summary>
public static class PanelDataExporter
{
    /// <summary>Колонки листа «Теги» — как в таблице HMI tags в TIA Portal.</summary>
    private static readonly string[] TagColumns =
    [
        "Name", "Data type", "Connection", "PLC name", "PLC tag", "Address",
        "Access mode", "Acquisition cycle", "Logged", "Source comment", "Comment"
    ];

    /// <summary>
    /// Записать книгу с листом «Теги»: строка заголовка и по строке на тег в порядке VAR.
    /// Заполнены Name и Data type, остальные колонки пустые — их данные ещё не найдены в файле.
    /// Текст пишется как есть (<c>=...</c> не становится формулой).
    /// </summary>
    /// <returns>Число тегов.</returns>
    public static int Export(IReadOnlyList<HmiTag> tags, string xlsxPath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Теги");
        for (var c = 0; c < TagColumns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = TagColumns[c];
        }

        var row = 2;
        foreach (var tag in tags)
        {
            sheet.Cell(row, 1).SetValue(tag.Name);
            sheet.Cell(row, 2).SetValue(TagTypeNames.Format(tag));
            row++;
        }
        workbook.SaveAs(xlsxPath);
        return tags.Count;
    }
}
