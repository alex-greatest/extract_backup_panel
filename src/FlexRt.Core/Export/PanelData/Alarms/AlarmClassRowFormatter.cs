using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Значения колонок класса аларма после Display name: Name, State machine, Log, E-mail address и цвета фона.</summary>
public sealed class AlarmClassRowFormatter
{
    /// <summary>Значение <see cref="AlarmClass.StateMachine"/> «с квитированием» (байты <c>0c 03</c>).</summary>
    private const int SingleModeAcknowledgment = 0x030c;

    /// <summary>Значение <see cref="AlarmClass.StateMachine"/> «без квитирования» (байты <c>04 01</c>).</summary>
    private const int WithoutAcknowledgment = 0x0104;

    /// <summary>Признак цвета (старший байт u32), когда цвета классов выключены в Runtime settings панели.</summary>
    private const long ClassColorsOff = 0x20;

    /// <summary>Текст ячейки цвета, когда цвета классов выключены.</summary>
    private const string ClassColorsOffText = "цвета классов выключены";

    /// <summary>Текст ячейки Log, когда класс не пишется в журнал, — как в TIA.</summary>
    private const string NoLog = "<No log>";

    /// <summary>Имя журнала алармов по ID класса.</summary>
    private readonly Dictionary<int, string> _logs = [];

    /// <summary>Адрес письма по ID класса.</summary>
    private readonly Dictionary<int, string> _emails = [];

    /// <summary>Значение Log класса без журнала: <c>&lt;No log&gt;</c>, а если RECMSG или ARCHIVES прочитана не целиком — <c>?</c>.</summary>
    private readonly string _noLog;

    /// <summary>Значение E-mail класса без адреса: пусто, а если MSG_LOGGER или FUNC_SEND_EMAIL прочитана не целиком — <c>?</c>.</summary>
    private readonly string _noEmail;

    /// <summary>Собрать журналы (ARCHIVES → RECMSG) и адреса (MSG_LOGGER → FUNC_SEND_EMAIL) по ID класса.</summary>
    public AlarmClassRowFormatter(FwxDocument doc)
    {
        IndexLogs(doc);
        IndexEmails(doc);
        _noLog = doc is { AlarmLogClassesComplete: true, ArchivesComplete: true } ? NoLog : "?";
        _noEmail = doc is { AlarmClassFunctionsComplete: true, EmailFunctionsComplete: true } ? "" : "?";
    }

    /// <summary>
    /// Ячейки класса: Name (стандартный класс — имя по ID, свой — <c>?</c>: его имени в панели нет),
    /// State machine (<c>Alarm with single-mode acknowledgment</c>, <c>Alarm without
    /// acknowledgment</c>, другой код — <c>?</c>), Log (имя журнала, нет — <c>&lt;No log&gt;</c>),
    /// E-mail address (нет — пусто). Если таблица журналов или писем прочитана не целиком, вместо
    /// <c>&lt;No log&gt;</c> и пустого адреса — <c>?</c>: журнал или адрес мог быть, но не прочитан и четыре цвета фона (<see cref="Color"/>).
    /// </summary>
    /// <returns>Ячейки в порядке колонок.</returns>
    public IEnumerable<ReportCell> Format(AlarmClass alarmClass)
    {
        yield return AlarmClassName.StandardName(alarmClass.Id) ?? "?";
        yield return StateMachine(alarmClass.StateMachine);
        yield return _logs.GetValueOrDefault(alarmClass.Id) ?? _noLog;
        yield return _emails.GetValueOrDefault(alarmClass.Id) ?? _noEmail;
        foreach (var color in alarmClass.BackgroundColors)
        {
            yield return Color(color);
        }
    }

    /// <summary>Текст State machine по двум байтам из файла.</summary>
    /// <returns>Режим, как в TIA; незнакомый код — <c>?</c>.</returns>
    private static string StateMachine(int value) => value switch
    {
        SingleModeAcknowledgment => "Alarm with single-mode acknowledgment",
        WithoutAcknowledgment => "Alarm without acknowledgment",
        _ => "?"
    };

    /// <summary>
    /// Ячейка цвета фона по u32 из файла: признак 0 — <c>255, 0, 0</c> с заливкой этим цветом;
    /// <see cref="ClassColorsOff"/> — <see cref="ClassColorsOffText"/> без заливки (цвета в файле
    /// нет: сверено на Project1, V21 — флажок цветов классов в Runtime settings); другой признак —
    /// <c>?</c>.
    /// </summary>
    /// <returns>Ячейка цвета.</returns>
    private static ReportCell Color(long value)
    {
        var flag = value >> 24;
        if (flag == ClassColorsOff)
        {
            return ClassColorsOffText;
        }
        if (flag != 0)
        {
            return "?";
        }
        int r = (int)(value & 0xff), g = (int)((value >> 8) & 0xff), b = (int)((value >> 16) & 0xff);
        ReportCell cell = $"{r}, {g}, {b}";
        return cell with { FillRgb = r << 16 | g << 8 | b };
    }

    /// <summary>
    /// Журналы классов: журнал ARCHIVES со ссылкой на элемент RECMSG — для каждого ID класса в этом
    /// элементе. Если журналов у класса несколько, берётся первый в порядке ARCHIVES.
    /// </summary>
    private void IndexLogs(FwxDocument doc)
    {
        var recMsg = doc.FindTable("RECMSG")?.Id;
        var classesByItem = doc.AlarmLogClasses.ToDictionary(l => l.Index, l => l.ClassIds);
        foreach (var archive in doc.Archives)
        {
            var ids = archive.References
                .Where(r => r.Table == recMsg)
                .SelectMany(r => classesByItem.GetValueOrDefault(r.Index) ?? []);
            foreach (var id in ids)
            {
                _logs.TryAdd(id, archive.Name);
            }
        }
    }

    /// <summary>
    /// Адреса классов: запись MSG_LOGGER на таблицу FUNC_SEND_EMAIL — адрес её элемента. Если
    /// адресов у класса несколько, берётся первый в порядке MSG_LOGGER.
    /// </summary>
    private void IndexEmails(FwxDocument doc)
    {
        var sendEmail = doc.FindTable("FUNC_SEND_EMAIL")?.Id;
        var addresses = doc.EmailFunctions.ToDictionary(e => e.Index, e => e.Address);
        foreach (var function in doc.AlarmClassFunctions.Where(f => f.Table == sendEmail))
        {
            if (addresses.TryGetValue(function.Index, out var address))
            {
                _emails.TryAdd(function.ClassId, address);
            }
        }
    }
}
