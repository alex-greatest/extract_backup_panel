using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Таблица листа «Классы алармов»: классы панели с колонками как Alarm classes в TIA.</summary>
public static class AlarmClassesTable
{
    /// <summary>Колонки после Display name — как в таблице Alarm classes в TIA.</summary>
    private static readonly string[] MiddleColumns =
    [
        "Name", "State machine", "Log", "E-mail address",
        "Background color \"Incoming\"", "Background color \"Incoming/Outgoing\"",
        "Background color \"Incoming/Acknowledged\"", "Background color \"Incoming/Outgoing/Acknowledged\""
    ];

    /// <summary>
    /// Классы панели: из MSG_NUMHANDLER, а если там их нет — из MSG_ANALOGHANDL (наблюдение: в обеих
    /// таблицах одни и те же классы). Классов System и Diagnosis events в этих таблицах нет.
    /// </summary>
    /// <returns>Классы в порядке файла.</returns>
    public static IReadOnlyList<AlarmClass> Classes(FwxDocument doc) =>
        doc.AlarmClasses.Count > 0 ? doc.AlarmClasses : doc.AnalogAlarmClasses;

    /// <summary>
    /// Построить таблицу. Колонки: <c>ID</c>, Display name на каждом языке в порядке STRINGSTORE
    /// (<c>Display name 0x409 — English (United States)</c>), <see cref="MiddleColumns"/>
    /// (<see cref="AlarmClassRowFormatter.Format"/>) и тексты состояний Incoming, Outgoing,
    /// Acknowledged на каждом языке. Строки — по классу из <see cref="Classes"/>, по возрастанию ID.
    /// </summary>
    /// <returns>Заголовки и строки классов.</returns>
    public static ReportTable Build(FwxDocument doc)
    {
        var texts = new AlarmLanguageTexts(doc);
        var formatter = new AlarmClassRowFormatter(doc);
        var languages = texts.Columns.ToList();
        List<string> columns =
        [
            "ID",
            .. languages.Select(l => $"Display name {l}"),
            .. MiddleColumns,
            .. languages.Select(l => $"Incoming {l}"),
            .. languages.Select(l => $"Outgoing {l}"),
            .. languages.Select(l => $"Acknowledged {l}")
        ];
        var rows = Classes(doc).OrderBy(c => c.Id).Select(Row).ToList();
        return new ReportTable(columns, rows);

        // строка одного класса: ID, Display name, колонки TIA, тексты состояний
        ReportCell[] Row(AlarmClass alarmClass) =>
        [
            alarmClass.Id,
            .. texts.PlainCells(alarmClass.DisplayNameIdx),
            .. formatter.Format(alarmClass),
            .. texts.PlainCells(alarmClass.IncomingTextIdx),
            .. texts.PlainCells(alarmClass.OutgoingTextIdx),
            .. texts.PlainCells(alarmClass.AcknowledgedTextIdx)
        ];
    }
}
