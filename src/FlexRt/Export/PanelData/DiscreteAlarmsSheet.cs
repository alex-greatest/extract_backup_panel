using ClosedXML.Excel;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Дискретные алармы»: алармы панели с колонками как Discrete alarms в TIA, без имени аларма.</summary>
public static class DiscreteAlarmsSheet
{
    /// <summary>Колонки после текстов — как в таблице Discrete alarms в TIA, плюс квитирование из ПЛК.</summary>
    private static readonly string[] TailColumns =
    [
        "Alarm class", "Trigger tag", "Trigger bit", "Trigger address",
        "HMI acknowledgment tag", "HMI acknowledgment bit", "HMI acknowledgment address",
        "PLC acknowledgment tag", "PLC acknowledgment bit", "Report"
    ];

    /// <summary>
    /// Добавить лист «Дискретные алармы» в книгу. Строка 1 — заголовок: <c>ID</c>, по колонке
    /// текста на язык в порядке STRINGSTORE (<c>0x409 — English (United States)</c>) и колонки
    /// <see cref="TailColumns"/>. Ниже — по строке на аларм, по возрастанию ID. Текст — строка
    /// языка с номером <see cref="DiscreteAlarm.TextIdx"/> с подписями полей
    /// (<see cref="AlarmText.Format"/>), нет строки — ячейка пустая; остальное —
    /// <see cref="AlarmRowFormatter.Format"/>. Колонки Name нет: имени аларма нет ни в панели, ни
    /// в ПЛК. Текст пишется как есть (<c>=...</c> не становится формулой).
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc, IReadOnlyList<PlcMatch?> matches)
    {
        var sheet = workbook.Worksheets.Add("Дискретные алармы");
        var languages = doc.Strings.Select(s => s.Lcid).Distinct().ToList();
        var texts = doc.Strings.ToLookup(s => (s.Lcid, (long)s.Idx), s => s.Str);

        WriteHeader(sheet, languages);
        var tagIndex = new AlarmTagIndex(doc);
        var fieldNames = new AlarmFieldNames(doc);
        var r = 2;
        foreach (var alarm in doc.DiscreteAlarms.OrderBy(a => a.Id))
        {
            sheet.Cell(r, 1).Value = alarm.Id;
            var labels = AlarmText.FieldLabels(alarm, fieldNames);
            for (var c = 0; c < languages.Count; c++)
            {
                var text = Text(languages[c], alarm.TextIdx);
                if (text.Length > 0)
                {
                    sheet.Cell(r, c + 2).SetValue(ExcelCell.Text(AlarmText.Format(text, labels)));
                }
            }
            var values = AlarmRowFormatter.Format(doc, matches, tagIndex, FirstLanguageText, alarm);
            WriteValues(sheet, r, languages.Count + 2, values);
            r++;
        }
        return;

        // строка языка lcid с номером idx; нет такой строки — пусто
        string Text(int lcid, long idx) => texts[(lcid, idx)].FirstOrDefault() ?? "";
        // строка с номером idx на первом языке панели (Display name своего класса); языков нет — пусто
        string FirstLanguageText(long idx) => languages.Count == 0 ? "" : Text(languages[0], idx);
    }

    /// <summary>Строка заголовка.</summary>
    private static void WriteHeader(IXLWorksheet sheet, List<int> languages)
    {
        sheet.Cell(1, 1).Value = "ID";
        for (var c = 0; c < languages.Count; c++)
        {
            sheet.Cell(1, c + 2).Value = LanguageText.Format(languages[c]);
        }
        for (var c = 0; c < TailColumns.Length; c++)
        {
            sheet.Cell(1, languages.Count + 2 + c).Value = TailColumns[c];
        }
    }

    /// <summary>
    /// Записать значения подряд с колонки <paramref name="first"/>: текст — через
    /// <see cref="ExcelCell.Text"/>, пустой текст не пишется; число — числом.
    /// </summary>
    private static void WriteValues(IXLWorksheet sheet, int row, int first, XLCellValue[] values)
    {
        for (var c = 0; c < values.Length; c++)
        {
            if (!values[c].IsText)
            {
                sheet.Cell(row, first + c).Value = values[c];
                continue;
            }
            var text = values[c].GetText();
            if (text.Length > 0)
            {
                sheet.Cell(row, first + c).SetValue(ExcelCell.Text(text));
            }
        }
    }
}
