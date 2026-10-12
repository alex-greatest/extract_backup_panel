namespace FlexRt.Model.Panel;

/// <summary>Класс аларма (HMI alarms → Alarm classes в TIA): запись списка классов таблицы MSG_NUMHANDLER.</summary>
/// <param name="Id">ID класса, как в свойствах класса в TIA: 1 — Errors, 2 — Warnings, 33 — Acknowledgement, 34 — No Acknowledgement, свои — свои номера.</param>
/// <param name="DisplayNameIdx">Номер строки Display name класса в STRINGSTORE, один для всех языков.</param>
public sealed record AlarmClass(int Id, long DisplayNameIdx);
