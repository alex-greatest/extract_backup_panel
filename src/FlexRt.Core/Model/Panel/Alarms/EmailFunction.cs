namespace FlexRt.Model.Panel.Alarms;

/// <summary>Отправка письма для класса аларма: элемент таблицы FUNC_SEND_EMAIL.</summary>
/// <param name="Index">Номер элемента в таблице, с нуля; на него ссылается запись MSG_LOGGER.</param>
/// <param name="Address">Адрес получателя (E-mail address класса в TIA).</param>
public sealed record EmailFunction(int Index, string Address);
