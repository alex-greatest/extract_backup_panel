namespace FlexRt.Model.Panel.Alarms;

/// <summary>Журнал панели (журнал алармов или данных): элемент таблицы ARCHIVES.</summary>
/// <param name="Index">Номер элемента в таблице, с нуля.</param>
/// <param name="Name">Имя журнала, как в TIA (<c>OP70_AlarmsHistory</c>).</param>
/// <param name="References">Ссылки элемента на элементы других таблиц (у журнала алармов — на элемент RECMSG).</param>
public sealed record PanelArchive(int Index, string Name, IReadOnlyList<VarReference> References);
