namespace FlexRt.Model.Panel.Alarms;

/// <summary>Класс аларма (HMI alarms → Alarm classes в TIA): запись списка классов таблицы MSG_NUMHANDLER или MSG_ANALOGHANDL.</summary>
/// <param name="Id">ID класса, как в свойствах класса в TIA: 1 — Errors, 2 — Warnings, 33 — Acknowledgement, 34 — No Acknowledgement, свои — свои номера.</param>
/// <param name="DisplayNameIdx">Номер строки Display name класса в STRINGSTORE, один для всех языков.</param>
/// <param name="IncomingTextIdx">Номер строки текста состояния Incoming в STRINGSTORE.</param>
/// <param name="OutgoingTextIdx">Номер строки текста состояния Outgoing в STRINGSTORE.</param>
/// <param name="AcknowledgedTextIdx">Номер строки текста состояния Acknowledged в STRINGSTORE.</param>
/// <param name="BackgroundColors">
/// Четыре цвета фона как u32 из файла в порядке колонок Background color "Incoming",
/// "Incoming/Outgoing", "Incoming/Acknowledged", "Incoming/Outgoing/Acknowledged": байты R, G, B и
/// признак (0 — цвет задан, <c>0x20</c> — цвета классов выключены в Runtime settings).
/// </param>
/// <param name="StateMachine">Два байта State machine как u16 из файла: <c>0x030c</c> — с квитированием, <c>0x0104</c> — без.</param>
public sealed record AlarmClass(
    int Id,
    long DisplayNameIdx,
    long IncomingTextIdx,
    long OutgoingTextIdx,
    long AcknowledgedTextIdx,
    IReadOnlyList<long> BackgroundColors,
    int StateMachine);
