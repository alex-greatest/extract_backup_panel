using ClosedXML.Excel;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Теги»: статистика тегов и таблица тегов с колонками как в HMI tags в TIA Portal.</summary>
public static class TagsSheet
{
    /// <summary>Колонки листа «Теги» — как в таблице HMI tags в TIA Portal.</summary>
    private static readonly string[] TagColumns =
    [
        "Name", "Data type", "Connection", "PLC name", "PLC tag", "Address",
        "Access mode", "Acquisition cycle", "Logged", "Source comment"
    ];

    /// <summary>Строка заголовка таблицы тегов: под статистикой и двумя пустыми строками.</summary>
    internal const int HeaderRow = 10;

    /// <summary>
    /// Добавить лист «Теги» в книгу: сверху статистика (<see cref="TagStatistics.Rows"/>, строки
    /// 1–7: подпись в A, число в B), две пустые строки, с <see cref="HeaderRow"/> — строка
    /// заголовка и по строке на тег в порядке VAR (значения — <see cref="TagRowFormatter.Format"/>).
    /// Пустые значения не пишутся. Текст пишется как есть (<c>=...</c> не становится формулой).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc, IReadOnlyList<PlcMatch?> matches)
    {
        var sheet = workbook.Worksheets.Add("Теги");
        var statistics = TagStatistics.Rows(doc, matches);
        for (var r = 0; r < statistics.Count; r++)
        {
            sheet.Cell(r + 1, 1).Value = statistics[r].Label;
            sheet.Cell(r + 1, 2).Value = statistics[r].Count;
        }
        for (var c = 0; c < TagColumns.Length; c++)
        {
            sheet.Cell(HeaderRow, c + 1).Value = TagColumns[c];
        }

        for (var i = 0; i < doc.Tags.Count; i++)
        {
            var values = TagRowFormatter.Format(doc, doc.Tags[i], i < matches.Count ? matches[i] : null);
            WriteRow(sheet, HeaderRow + 1 + i, values);
        }
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
}
