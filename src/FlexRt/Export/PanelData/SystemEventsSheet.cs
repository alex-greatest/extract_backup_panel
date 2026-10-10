using ClosedXML.Excel;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Системные события»: номер события и его текст на каждом языке панели, как System events в TIA.</summary>
public static class SystemEventsSheet
{
    /// <summary>
    /// Добавить лист «Системные события» в книгу. Строка 1 — заголовок: <c>ID</c> и по колонке на
    /// язык в порядке STRINGSTORE (<c>0x409 — English (United States)</c>). Ниже — по строке на
    /// событие, по возрастанию ID; текст — строка языка с номером <see cref="SystemEvent.TextIdx"/>,
    /// нет такой строки — ячейка пустая. Текст пишется как есть (<c>=...</c> не становится формулой).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc)
    {
        var sheet = workbook.Worksheets.Add("Системные события");
        var languages = doc.Strings.Select(s => s.Lcid).Distinct().ToList();
        var texts = doc.Strings.ToLookup(s => (s.Lcid, (long)s.Idx), s => s.Str);
        sheet.Cell(1, 1).Value = "ID";
        for (var c = 0; c < languages.Count; c++)
        {
            sheet.Cell(1, c + 2).Value = LanguageText.Format(languages[c]);
        }

        var r = 2;
        foreach (var systemEvent in doc.SystemEvents.OrderBy(e => e.Id))
        {
            sheet.Cell(r, 1).Value = systemEvent.Id;
            for (var c = 0; c < languages.Count; c++)
            {
                var text = texts[(languages[c], systemEvent.TextIdx)].FirstOrDefault();
                if (!string.IsNullOrEmpty(text))
                {
                    sheet.Cell(r, c + 2).SetValue(ExcelCell.Text(text));
                }
            }
            r++;
        }
    }
}
