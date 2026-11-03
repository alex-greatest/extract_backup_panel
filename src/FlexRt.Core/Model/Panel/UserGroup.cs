namespace FlexRt.Model.Panel;

/// <summary>
/// Группа пользователей панели (User administration → User groups в TIA) из таблицы SECURITY_UNIT. Name группы в файл не
/// попадает, только Display name; пользователи и пароли хранятся в другом файле (<c>pdata.pwx</c>) и не читаются.
/// </summary>
/// <param name="Number">Number группы.</param>
/// <param name="Rights">
/// Маска прав: бит k — право с номером k + 1 в TIA (бит 0 — User administration, 1 — Monitor, 2 — Operate).
/// </param>
/// <param name="TextIndex">Номер строки языков с Display name (<see cref="LangString.Idx"/>).</param>
public sealed record UserGroup(int Number, long Rights, long TextIndex);
