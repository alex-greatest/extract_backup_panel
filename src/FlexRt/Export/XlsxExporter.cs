using ClosedXML.Excel;
using FlexRt.Model;

namespace FlexRt.Export;

/// <summary>Языковые строки в Excel: лист на каждый LCID.</summary>
public static class XlsxExporter
{
    /// <summary>Предел длины текста в ячейке Excel.</summary>
    private const int MaxCellLength = 32767;

    /// <summary>
    /// Записать книгу: лист <c>0x&lt;lcid&gt;</c> с колонками idx и str. Текст пишется как есть
    /// (<c>=...</c> не становится формулой), строка длиннее предела ячейки обрезается.
    /// </summary>
    /// <returns>Число языков.</returns>
    public static int Export(IReadOnlyList<LangString> strings, string xlsxPath)
    {
        using var workbook = new XLWorkbook();
        var langs = strings.GroupBy(s => s.Lcid).ToList();
        foreach (var lang in langs)
        {
            var sheet = workbook.Worksheets.Add($"0x{lang.Key:x}");
            sheet.Cell(1, 1).Value = "idx";
            sheet.Cell(1, 2).Value = "str";
            var row = 2;
            foreach (var s in lang)
            {
                sheet.Cell(row, 1).Value = s.Idx;
                sheet.Cell(row, 2).SetValue(s.Str.Length > MaxCellLength ? s.Str[..MaxCellLength] : s.Str);
                row++;
            }
        }
        workbook.SaveAs(xlsxPath);
        return langs.Count;
    }
}
