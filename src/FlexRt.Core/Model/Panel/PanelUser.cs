namespace FlexRt.Model.Panel;

/// <summary>Пользователь из PWX/PWL: имя, номер группы и время автоматического выхода в секундах; ноль — выключен.</summary>
public sealed record PanelUser(string Name, long GroupNumber, long LogoffSeconds);
