namespace FlexRt.Model.Panel;

/// <summary>Дискретный аларм панели (HMI alarms → Discrete alarms в TIA): элемент таблицы MSG_NUMEVENT.</summary>
/// <param name="Id">Номер аларма, как в колонке ID в TIA.</param>
/// <param name="TextIdx">Номер строки текста аларма в STRINGSTORE (<see cref="LangString.Idx"/>), один для всех языков.</param>
/// <param name="ClassIndex">Номер класса аларма в списке <see cref="FwxDocument.AlarmClasses"/>, с нуля.</param>
/// <param name="Report">Report = On в TIA.</param>
/// <param name="Fields">Поля вывода в тексте (<c>&lt;Tag : 6, …&gt;</c>, <c>&lt;Text list, …&gt;</c>) по порядку номеров в тексте.</param>
public sealed record DiscreteAlarm(long Id, long TextIdx, int ClassIndex, bool Report, IReadOnlyList<AlarmField> Fields);
