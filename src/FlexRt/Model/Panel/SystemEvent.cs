namespace FlexRt.Model.Panel;

/// <summary>Системное событие панели (HMI alarms → System events в TIA): запись таблицы SYSMSGHANDLER.</summary>
/// <param name="Id">Номер события, как в колонке ID в TIA (9999, 10000, …).</param>
/// <param name="TextIdx">Номер строки текста события в STRINGSTORE (<see cref="LangString.Idx"/>), один для всех языков.</param>
public sealed record SystemEvent(long Id, long TextIdx);
