namespace FlexRt.Model.Panel;

/// <summary>Результат поиска соседнего файла пользователей: первый прочитанный источник либо ошибка без пользователей.</summary>
public sealed record PanelUsers(string? Source, IReadOnlyList<PanelUser> Items, string? Error);
