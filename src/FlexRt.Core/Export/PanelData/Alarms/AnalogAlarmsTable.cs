using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Таблица листа «Аналоговые алармы»: алармы панели с колонками как Analog alarms в TIA, без имени аларма.</summary>
public static class AnalogAlarmsTable
{
    /// <summary>Колонки после текстов — как в таблице Analog alarms в TIA и Alarm group из свойств аларма.</summary>
    private static readonly string[] TailColumns = ["Alarm class", "Alarm group", "Trigger tag", "Limit", "Limit mode", "Report"];

    /// <summary>
    /// Построить таблицу. Колонки: <c>ID</c>, по колонке текста на язык в порядке STRINGSTORE
    /// (<c>0x409 — English (United States)</c>) и <see cref="TailColumns"/>. Строки — по аларму, по
    /// возрастанию ID. Текст — <see cref="AlarmLanguageTexts.Cells"/> по строке
    /// <see cref="AnalogAlarm.TextIdx"/>; остальное — <see cref="AnalogAlarmRowFormatter.Format"/>.
    /// Колонки Name нет: имени аларма нет ни в панели, ни в ПЛК. Данные ПЛК не нужны: тег
    /// срабатывания и тег предела — HMI-теги панели.
    /// </summary>
    /// <returns>Заголовки и строки алармов.</returns>
    public static ReportTable Build(FwxDocument doc)
    {
        var texts = new AlarmLanguageTexts(doc);
        var formatter = new AnalogAlarmRowFormatter(doc);
        List<string> columns = ["ID", .. texts.Columns, .. TailColumns];
        var rows = doc.AnalogAlarms.OrderBy(a => a.Id).Select(Row).ToList();
        return new ReportTable(columns, rows);

        // строка одного аларма: ID, тексты на языках, колонки после текстов
        ReportCell[] Row(AnalogAlarm alarm) =>
        [
            alarm.Id,
            .. texts.Cells(alarm.TextIdx, alarm.Fields),
            .. formatter.Format(alarm, texts.FirstLanguageText)
        ];
    }
}
