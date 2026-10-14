using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Таблица листа «Дискретные алармы»: алармы панели с колонками как Discrete alarms в TIA, без имени аларма.</summary>
public static class DiscreteAlarmsTable
{
    /// <summary>Колонки после текстов — как в таблице Discrete alarms в TIA и Alarm group из свойств аларма, плюс квитирование из ПЛК.</summary>
    private static readonly string[] TailColumns =
    [
        "Alarm class", "Alarm group", "Trigger tag", "Trigger bit", "Trigger address",
        "HMI acknowledgment tag", "HMI acknowledgment bit", "HMI acknowledgment address",
        "PLC acknowledgment tag", "PLC acknowledgment bit", "Report"
    ];

    /// <summary>
    /// Построить таблицу. Колонки: <c>ID</c>, по колонке текста на язык в порядке STRINGSTORE
    /// (<c>0x409 — English (United States)</c>) и <see cref="TailColumns"/>. Строки — по аларму, по
    /// возрастанию ID. Текст — <see cref="AlarmLanguageTexts.Cells"/> по строке
    /// <see cref="DiscreteAlarm.TextIdx"/>; остальное — <see cref="AlarmRowFormatter.Format"/>.
    /// Колонки Name нет: имени аларма нет ни в панели, ни в ПЛК.
    /// </summary>
    /// <returns>Заголовки и строки алармов.</returns>
    public static ReportTable Build(FwxDocument doc, IReadOnlyList<PlcMatch?> matches)
    {
        var texts = new AlarmLanguageTexts(doc);
        var tagIndex = new AlarmTagIndex(doc);
        List<string> columns = ["ID", .. texts.Columns, .. TailColumns];
        var rows = doc.DiscreteAlarms.OrderBy(a => a.Id).Select(Row).ToList();
        return new ReportTable(columns, rows);

        // строка одного аларма: ID, тексты на языках, колонки после текстов
        ReportCell[] Row(DiscreteAlarm alarm) =>
        [
            alarm.Id,
            .. texts.Cells(alarm.TextIdx, alarm.Fields),
            .. AlarmRowFormatter.Format(doc, matches, tagIndex, texts.FirstLanguageText, alarm)
        ];
    }
}
