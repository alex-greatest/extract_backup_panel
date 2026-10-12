using ClosedXML.Excel;

namespace FlexRt.Export;

/// <summary>Запись <see cref="ReportTable"/> на лист Excel.</summary>
internal static class ReportTableWriter
{
    /// <summary>
    /// Записать таблицу с колонки A: заголовки — в строку <paramref name="headerRow"/>, строки —
    /// ниже подряд. Число пишется числом; текст — через <see cref="ExcelCell.Text"/> (как есть,
    /// <c>=...</c> не становится формулой, длинный обрезается); пустой текст не пишется.
    /// </summary>
    public static void Write(IXLWorksheet sheet, int headerRow, ReportTable table)
    {
        for (var c = 0; c < table.Columns.Count; c++)
        {
            sheet.Cell(headerRow, c + 1).Value = table.Columns[c];
        }
        for (var r = 0; r < table.Rows.Count; r++)
        {
            WriteRow(sheet, headerRow + 1 + r, table.Rows[r]);
        }
    }

    /// <summary>Записать ячейки одной строки с колонки A; пустой текст пропускается.</summary>
    private static void WriteRow(IXLWorksheet sheet, int row, ReportCell[] cells)
    {
        for (var c = 0; c < cells.Length; c++)
        {
            if (cells[c].Number is { } number)
            {
                sheet.Cell(row, c + 1).Value = number;
            }
            else if (cells[c].Text is { Length: > 0 } text)
            {
                sheet.Cell(row, c + 1).SetValue(ExcelCell.Text(text));
            }
        }
    }
}
