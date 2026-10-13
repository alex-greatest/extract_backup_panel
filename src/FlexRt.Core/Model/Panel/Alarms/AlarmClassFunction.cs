namespace FlexRt.Model.Panel.Alarms;

/// <summary>Функция, привязанная к классу аларма: запись таблицы MSG_LOGGER (у E-mail address — элемент FUNC_SEND_EMAIL).</summary>
/// <param name="ClassId">ID класса (<see cref="AlarmClass.Id"/>).</param>
/// <param name="Table">Id таблицы функции из TOC.</param>
/// <param name="Index">Номер элемента в таблице функции, с нуля.</param>
public sealed record AlarmClassFunction(int ClassId, int Table, int Index);
