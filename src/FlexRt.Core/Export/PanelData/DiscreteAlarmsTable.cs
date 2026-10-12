using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Таблица листа «Дискретные алармы»: алармы панели с колонками как Discrete alarms в TIA, без имени аларма.</summary>
public static class DiscreteAlarmsTable
{
    /// <summary>Колонки после текстов — как в таблице Discrete alarms в TIA, плюс квитирование из ПЛК.</summary>
    private static readonly string[] TailColumns =
    [
        "Alarm class", "Trigger tag", "Trigger bit", "Trigger address",
        "HMI acknowledgment tag", "HMI acknowledgment bit", "HMI acknowledgment address",
        "PLC acknowledgment tag", "PLC acknowledgment bit", "Report"
    ];

    /// <summary>
    /// Построить таблицу. Колонки: <c>ID</c>, по колонке текста на язык в порядке STRINGSTORE
    /// (<c>0x409 — English (United States)</c>) и <see cref="TailColumns"/>. Строки — по аларму, по
    /// возрастанию ID. Текст — строка языка с номером <see cref="DiscreteAlarm.TextIdx"/> с
    /// подписями полей (<see cref="AlarmText.Format"/>), нет строки — пустая ячейка; остальное —
    /// <see cref="AlarmRowFormatter.Format"/>. Колонки Name нет: имени аларма нет ни в панели, ни
    /// в ПЛК.
    /// </summary>
    /// <returns>Заголовки и строки алармов.</returns>
    public static ReportTable Build(FwxDocument doc, IReadOnlyList<PlcMatch?> matches)
    {
        var languages = doc.Strings.Select(s => s.Lcid).Distinct().ToList();
        var texts = doc.Strings.ToLookup(s => (s.Lcid, (long)s.Idx), s => s.Str);
        var columns = languages.Select(LanguageText.Format).Prepend("ID").Concat(TailColumns).ToList();
        var tagIndex = new AlarmTagIndex(doc);
        var fieldNames = new AlarmFieldNames(doc);
        var rows = doc.DiscreteAlarms.OrderBy(a => a.Id).Select(Row).ToList();
        return new ReportTable(columns, rows);

        // строка одного аларма: ID, тексты на языках, колонки после текстов
        ReportCell[] Row(DiscreteAlarm alarm)
        {
            var labels = AlarmText.FieldLabels(alarm, fieldNames);
            var alarmTexts = languages.Select(lcid => Text(lcid, alarm.TextIdx))
                .Select(text => (ReportCell)(text.Length > 0 ? AlarmText.Format(text, labels) : ""));
            var tail = AlarmRowFormatter.Format(doc, matches, tagIndex, FirstLanguageText, alarm);
            return alarmTexts.Prepend(alarm.Id).Concat(tail).ToArray();
        }
        // строка языка lcid с номером idx; нет такой строки — пусто
        string Text(int lcid, long idx) => texts[(lcid, idx)].FirstOrDefault() ?? "";
        // строка с номером idx на первом языке панели (Display name своего класса); языков нет — пусто
        string FirstLanguageText(long idx) => languages.Count == 0 ? "" : Text(languages[0], idx);
    }
}
