using ClosedXML.Excel;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.Tags;

/// <summary>Данные панели в Excel: лист «Теги» и, если есть ошибки файла ПЛК, лист «Ошибки».</summary>
public static class PanelDataExporter
{
    /// <summary>Колонки листа «Теги» — как в таблице HMI tags в TIA Portal.</summary>
    private static readonly string[] TagColumns =
    [
        "Name", "Data type", "Connection", "PLC name", "PLC tag", "Address",
        "Access mode", "Acquisition cycle", "Logged", "Source comment", "Comment"
    ];

    /// <summary>
    /// Записать книгу: лист «Теги» — строка заголовка и по строке на тег в порядке VAR
    /// (значения — <see cref="TagRowFormatter.Format"/>); лист «Ошибки» — только если
    /// <paramref name="errors"/> не пуст, по строке на сообщение. Пустые значения не
    /// пишутся. Текст пишется как есть (<c>=...</c> не становится формулой).
    /// </summary>
    /// <returns>Число тегов.</returns>
    /// <exception cref="IOException">Файл не удалось записать (например, открыт в Excel).</exception>
    public static int Export(FwxDocument doc, IReadOnlyList<PlcMatch?> matches, IReadOnlyList<string> errors, string xlsxPath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Теги");
        for (var c = 0; c < TagColumns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = TagColumns[c];
        }

        for (var i = 0; i < doc.Tags.Count; i++)
        {
            var values = TagRowFormatter.Format(doc, doc.Tags[i], i < matches.Count ? matches[i] : null);
            WriteRow(sheet, i + 2, values);
        }
        if (errors.Count > 0)
        {
            WriteErrors(workbook, errors);
        }
        workbook.SaveAs(xlsxPath);
        return doc.Tags.Count;
    }

    /// <summary>Записать непустые значения строки, начиная с колонки A; длинный текст обрезается по <see cref="ExcelCell.Text"/>.</summary>
    private static void WriteRow(IXLWorksheet sheet, int row, string[] values)
    {
        for (var c = 0; c < values.Length; c++)
        {
            if (values[c].Length > 0)
            {
                sheet.Cell(row, c + 1).SetValue(ExcelCell.Text(values[c]));
            }
        }
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
