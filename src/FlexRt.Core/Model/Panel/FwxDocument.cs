using FlexRt.Binary;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Model.Panel;

/// <summary>Разобранный FWX: заголовок, TOC, строки STRINGSTORE, теги VAR и предупреждения.</summary>
public sealed class FwxDocument
{
    /// <summary>Байты файла целиком.</summary>
    public required FwxBinary Binary { get; init; }
    /// <summary>Заголовок файла.</summary>
    public required FwxHeader Header { get; init; }
    /// <summary>Непустые записи Table Of Contents в порядке TOC.</summary>
    public List<TocEntry> Toc { get; } = [];
    /// <summary>Строки всех языков из STRINGSTORE в порядке каталога.</summary>
    public List<LangString> Strings { get; } = [];
    /// <summary>Теги HMI из VAR в порядке каталога.</summary>
    public List<HmiTag> Tags { get; } = [];
    /// <summary>Связи PLC-тегов с ПЛК из DATALINK_READWR в порядке каталога.</summary>
    public List<PlcLink> Links { get; } = [];
    /// <summary>
    /// Элементы DATALINK в порядке каталога: служебные связи соединений и указатели областей
    /// (на них ссылается, например, тег <c>Screen Number</c>).
    /// </summary>
    public List<PlcLink> AreaLinks { get; } = [];
    /// <summary>Соединения панели с ПЛК из CONNECTION_OMSP в порядке каталога.</summary>
    public List<HmiConnection> Connections { get; } = [];
    /// <summary>Сетевые параметры панели из DEVICE_OMSP в порядке каталога (в файлах — один элемент).</summary>
    public List<HmiDevice> Devices { get; } = [];
    /// <summary>Системные события из SYSMSGHANDLER в порядке файла.</summary>
    public List<SystemEvent> SystemEvents { get; } = [];
    /// <summary>Дискретные алармы из MSG_NUMEVENT в порядке файла.</summary>
    public List<DiscreteAlarm> DiscreteAlarms { get; } = [];
    /// <summary>Классы алармов из MSG_NUMHANDLER в порядке файла; на номер в списке ссылается <see cref="DiscreteAlarm.ClassIndex"/>.</summary>
    public List<AlarmClass> AlarmClasses { get; } = [];
    /// <summary>Теги срабатывания и квитирования дискретных алармов из AREAPTR в порядке каталога.</summary>
    public List<AlarmBits> AlarmBits { get; } = [];
    /// <summary>
    /// AREAPTR прочитана целиком (или её нет): аларм без тега какого-то назначения действительно
    /// без тега. <c>false</c> — элементы пропущены или разбор оборвался, отсутствие тега не известно.
    /// </summary>
    public bool AlarmBitsComplete { get; set; }
    /// <summary>Аналоговые алармы из MSG_ANALOGCREAT в порядке файла.</summary>
    public List<AnalogAlarm> AnalogAlarms { get; } = [];
    /// <summary>Классы аналоговых алармов из MSG_ANALOGHANDL в порядке файла; на номер в списке ссылается <see cref="AnalogAlarm.ClassIndex"/>.</summary>
    public List<AlarmClass> AnalogAlarmClasses { get; } = [];
    /// <summary>Пределы аналоговых алармов из MSG_ANALOG_OBSE в порядке каталога.</summary>
    public List<AnalogAlarmLimit> AnalogLimits { get; } = [];
    /// <summary>Списки классов журналов алармов из RECMSG в порядке каталога.</summary>
    public List<AlarmLogClasses> AlarmLogClasses { get; } = [];
    /// <summary>Журналы панели из ARCHIVES в порядке каталога.</summary>
    public List<PanelArchive> Archives { get; } = [];
    /// <summary>Функции классов алармов из MSG_LOGGER в порядке файла.</summary>
    public List<AlarmClassFunction> AlarmClassFunctions { get; } = [];
    /// <summary>Отправки писем из FUNC_SEND_EMAIL в порядке каталога.</summary>
    public List<EmailFunction> EmailFunctions { get; } = [];
    /// <summary>RECMSG прочитана целиком (или её нет); <c>false</c> — разбор оборвался.</summary>
    public bool AlarmLogClassesComplete { get; set; }
    /// <summary>ARCHIVES прочитана целиком (или её нет); <c>false</c> — разбор оборвался.</summary>
    public bool ArchivesComplete { get; set; }
    /// <summary>MSG_LOGGER прочитана целиком (или её нет); <c>false</c> — разбор оборвался.</summary>
    public bool AlarmClassFunctionsComplete { get; set; }
    /// <summary>FUNC_SEND_EMAIL прочитана целиком (или её нет); <c>false</c> — разбор оборвался.</summary>
    public bool EmailFunctionsComplete { get; set; }
    /// <summary>Таблицы, которые не удалось разобрать (остальное при этом читается).</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>Таблица по имени из TOC (<c>VAR</c>, <c>STRINGSTORE</c>, …) или <c>null</c>.</summary>
    public TocEntry? FindTable(string name) => Toc.FirstOrDefault(t => t.Name == name);
}
