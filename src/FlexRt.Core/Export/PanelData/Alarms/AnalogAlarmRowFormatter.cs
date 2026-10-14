using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Колонки листа «Аналоговые алармы» после текстов: класс, группа, тег срабатывания, предел, Limit mode и Report.</summary>
public sealed class AnalogAlarmRowFormatter
{
    /// <summary>Байт Limit mode = Higher; сверено с TIA на шаговых копиях A603A0097.</summary>
    private const int HigherMode = 1;

    /// <summary>Байт Limit mode = Lower; сверено с TIA на шаговых копиях A603A0097.</summary>
    private const int LowerMode = 0;

    /// <summary>Классы аналоговых алармов из MSG_ANALOGHANDL.</summary>
    private readonly IReadOnlyList<AlarmClass> _classes;

    /// <summary>Предел по ID аларма; если пределов с одним ID несколько — первый в порядке файла.</summary>
    private readonly Dictionary<long, AnalogAlarmLimit> _limits;

    /// <summary>Имя тега срабатывания по номеру элемента MSG_ANALOG_OBSE.</summary>
    private readonly Dictionary<int, string> _triggerByLimit = [];

    /// <summary>Имя тега по номеру записи VAR с нуля.</summary>
    private readonly Dictionary<int, string> _tagByVarIndex;

    /// <summary>
    /// Собрать индексы: тег срабатывания — тег, чья запись VAR ссылается на элемент
    /// MSG_ANALOG_OBSE (Id таблицы из TOC); если таких тегов несколько, берётся первый.
    /// </summary>
    public AnalogAlarmRowFormatter(FwxDocument doc)
    {
        _classes = doc.AnalogAlarmClasses;
        _limits = doc.AnalogLimits.GroupBy(l => l.AlarmId).ToDictionary(g => g.Key, g => g.First());
        _tagByVarIndex = doc.Tags.GroupBy(t => t.Index - 1).ToDictionary(g => g.Key, g => g.First().Name);
        var limitTable = doc.FindTable("MSG_ANALOG_OBSE")?.Id;
        foreach (var tag in doc.Tags)
        {
            foreach (var reference in tag.References.Where(r => r.Table == limitTable))
            {
                _triggerByLimit.TryAdd(reference.Index, tag.Name);
            }
        }
    }

    /// <summary>
    /// Значения колонок Alarm class, Alarm group, Trigger tag, Limit, Limit mode, Report. Класс —
    /// <see cref="AlarmClassName.Format"/> по списку классов MSG_ANALOGHANDL, группа —
    /// <see cref="AlarmGroupText.Format"/>. Нет элемента
    /// MSG_ANALOG_OBSE с ID аларма (таблица не прочитана) — Trigger tag, Limit и Limit mode
    /// <c>?</c>; ни один тег VAR не ссылается на элемент — Trigger tag <c>?</c>. Limit —
    /// <see cref="Limit"/>; Limit mode — <c>Higher</c>, <c>Lower</c>, незнакомый байт — <c>?</c>.
    /// </summary>
    /// <returns>6 значений: текст или число.</returns>
    public ReportCell[] Format(AnalogAlarm alarm, Func<long, string> firstLanguageText)
    {
        var className = AlarmClassName.Format(_classes, alarm.ClassIndex, firstLanguageText);
        var group = AlarmGroupText.Format(alarm.Group);
        var report = alarm.Report ? "On" : "Off";
        if (!_limits.TryGetValue(alarm.Id, out var limit))
        {
            return [className, group, "?", "?", "?", report];
        }
        return [className, group, _triggerByLimit.GetValueOrDefault(limit.Index, "?"), Limit(limit), Mode(limit.Mode), report];
    }

    /// <summary>
    /// Предел: есть тег предела — имя HMI-тега (записи нет среди тегов — <c>?</c>); иначе
    /// константа <see cref="AnalogLimitValue.Format"/>.
    /// </summary>
    /// <returns>Значение ячейки Limit.</returns>
    private ReportCell Limit(AnalogAlarmLimit limit)
    {
        if (limit.LimitTag is { } tag)
        {
            return _tagByVarIndex.GetValueOrDefault(tag, "?");
        }
        return AnalogLimitValue.Format(limit.ValueKind, limit.Value);
    }

    /// <summary>Limit mode по байту.</summary>
    /// <returns><c>Higher</c>, <c>Lower</c> или <c>?</c>.</returns>
    private static string Mode(int mode) => mode switch
    {
        HigherMode => "Higher",
        LowerMode => "Lower",
        _ => "?"
    };
}
