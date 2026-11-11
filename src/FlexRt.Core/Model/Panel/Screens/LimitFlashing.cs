namespace FlexRt.Model.Panel.Screens;

/// <summary>
/// Flash on limit violation I/O field (Properties → Flashing в TIA). TIA пишет флажок, только если у тега задан Range: без
/// Range копии с флажком и без него совпадают (шаги <c>data/real/steps-iofield/</c> 73–76).
/// </summary>
public enum LimitFlashing
{
    /// <summary>
    /// В файле нет: у тега не задан Range — у поля нет ни мигания, ни ссылки на цвета Limits в ADAPTVALUECHANG (или список
    /// там другого вида, не цвета).
    /// </summary>
    NotInFile,

    /// <summary>
    /// Флажок снят: поле ссылается на цвета Limits в ADAPTVALUECHANG (и тогда, когда элемент с цветами не сходится —
    /// ссылка есть, значит Range задан).
    /// </summary>
    Off,

    /// <summary>Флажок установлен: поле ссылается на мигание ADAPTDYNATTRIBU → SYMB → LIMIT.</summary>
    On,

    /// <summary>
    /// Не прочитано: элемент ADAPTDYNATTRIBU или SYMB не сходится (предупреждение); цвета Limits тогда — из
    /// ADAPTVALUECHANG, если они там есть, иначе «не прочитано».
    /// </summary>
    Unread,
}
