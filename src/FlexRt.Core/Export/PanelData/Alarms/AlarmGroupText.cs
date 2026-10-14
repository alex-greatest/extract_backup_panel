namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Значение колонки Alarm group листов дискретных и аналоговых алармов.</summary>
public static class AlarmGroupText
{
    /// <summary>Группа не назначена — как в TIA.</summary>
    private const string NoGroup = "<No alarm group>";

    /// <summary>
    /// ID группы числом; 0 — <c>&lt;No alarm group&gt;</c>. Имени группы (<c>Alarm_group_1</c>) в
    /// панели нет, поэтому выводится только ID.
    /// </summary>
    /// <returns>Число или текст.</returns>
    public static ReportCell Format(int group) => group == 0 ? NoGroup : group;
}
