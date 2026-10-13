using ClosedXML.Excel;

namespace FlexRt.Export;

/// <summary>Запись <see cref="ReportTable"/> на лист Excel.</summary>
internal static class ReportTableWriter
{
    /// <summary>
    /// Записать таблицу с колонки A: заголовки — в строку <paramref name="headerRow"/>, строки —
    /// ниже подряд. Число пишется числом; текст — через <see cref="ExcelCell.Text"/> (как есть,
    /// <c>=...</c> не становится формулой, длинный обрезается); пустой текст не пишется; заливка —
    /// по <see cref="ReportCell.FillRgb"/>.
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

    /// <summary>
    /// Записать ячейки одной строки с колонки A; пустой текст пропускается, у ячейки с
    /// <see cref="ReportCell.FillRgb"/> закрашивается фон (<see cref="Fill"/>).
    /// </summary>
    private static void WriteRow(IXLWorksheet sheet, int row, ReportCell[] cells)
    {
        for (var c = 0; c < cells.Length; c++)
        {
            if (cells[c].FillRgb is { } rgb)
            {
                Fill(sheet.Cell(row, c + 1), rgb);
            }
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

    /// <summary>Порог яркости заливки (0–255): темнее него (чёрный, тёмно-синий) чёрный текст не читается и пишется белым; красный (76) и зелёный 0, 128, 0 (75) остаются с чёрным текстом.</summary>
    private const int DarkFillBrightness = 64;

    /// <summary>
    /// Закрасить фон ячейки цветом <paramref name="rgb"/> (<c>0xRRGGBB</c>); на тёмной заливке
    /// (яркость 0.299·R + 0.587·G + 0.114·B ниже <see cref="DarkFillBrightness"/>, считается в
    /// целых, чтобы граница не зависела от округления) текст — белый.
    /// </summary>
    private static void Fill(IXLCell cell, int rgb)
    {
        int r = rgb >> 16, g = (rgb >> 8) & 0xff, b = rgb & 0xff;
        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(r, g, b);
        if (299 * r + 587 * g + 114 * b < DarkFillBrightness * 1000)
        {
            cell.Style.Font.FontColor = XLColor.White;
        }
    }
}
