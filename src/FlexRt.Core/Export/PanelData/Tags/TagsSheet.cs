using ClosedXML.Excel;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Tags;

/// <summary>Лист «Теги»: статистика тегов и таблица тегов с колонками как в HMI tags в TIA Portal.</summary>
public static class TagsSheet
{
    /// <summary>Имя листа в книге.</summary>
    public const string Name = "Теги";

    /// <summary>Строка заголовка таблицы тегов: под статистикой и двумя пустыми строками.</summary>
    internal const int HeaderRow = 10;

    /// <summary>
    /// Добавить лист «Теги» в книгу: сверху статистика (<see cref="TagStatistics.Rows"/>, строки
    /// 1–7: подпись в A, число в B), две пустые строки, с <see cref="HeaderRow"/> — таблица
    /// <see cref="TagsTable.Build"/> (<see cref="ReportTableWriter.Write"/>: пустые значения не
    /// пишутся, <c>=...</c> не становится формулой).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc, IReadOnlyList<PlcMatch?> matches) =>
        Write(workbook, TagStatistics.Rows(doc, matches), TagsTable.Build(doc, matches));

    /// <summary>
    /// Добавить лист «Теги» из готовых строк статистики (<see cref="TagStatistics.Rows"/>) и таблицы
    /// (<see cref="TagsTable.Build"/>): статистика с первой строки, таблица — с <see cref="HeaderRow"/>.
    /// </summary>
    public static void Write(XLWorkbook workbook, IReadOnlyList<(string Label, int Count)> statistics, ReportTable table)
    {
        var sheet = workbook.Worksheets.Add(Name);
        for (var r = 0; r < statistics.Count; r++)
        {
            sheet.Cell(r + 1, 1).Value = statistics[r].Label;
            sheet.Cell(r + 1, 2).Value = statistics[r].Count;
        }
        ReportTableWriter.Write(sheet, HeaderRow, table);
    }
}
