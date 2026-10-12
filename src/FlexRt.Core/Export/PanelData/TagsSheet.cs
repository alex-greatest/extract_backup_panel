using ClosedXML.Excel;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Теги»: статистика тегов и таблица тегов с колонками как в HMI tags в TIA Portal.</summary>
public static class TagsSheet
{
    /// <summary>Строка заголовка таблицы тегов: под статистикой и двумя пустыми строками.</summary>
    internal const int HeaderRow = 10;

    /// <summary>
    /// Добавить лист «Теги» в книгу: сверху статистика (<see cref="TagStatistics.Rows"/>, строки
    /// 1–7: подпись в A, число в B), две пустые строки, с <see cref="HeaderRow"/> — таблица
    /// <see cref="TagsTable.Build"/> (<see cref="ReportTableWriter.Write"/>: пустые значения не
    /// пишутся, <c>=...</c> не становится формулой).
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
        ReportTableWriter.Write(sheet, HeaderRow, TagsTable.Build(doc, matches));
    }
}
