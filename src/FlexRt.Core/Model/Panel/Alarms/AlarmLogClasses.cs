namespace FlexRt.Model.Panel.Alarms;

/// <summary>Классы алармов, которые пишутся в журнал алармов: элемент таблицы RECMSG.</summary>
/// <param name="Index">Номер элемента в таблице, с нуля; на него ссылается журнал в ARCHIVES.</param>
/// <param name="ClassIds">ID классов (<see cref="AlarmClass.Id"/>) в порядке файла.</param>
public sealed record AlarmLogClasses(int Index, IReadOnlyList<int> ClassIds);
