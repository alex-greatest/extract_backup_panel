namespace FlexRt.Model.Panel.Screens;

/// <summary>Настройки Appearance поля, прочитанные по цепочке ADAPTDYNATTRIBU → SYMB → VAR.</summary>
/// <param name="Tag">Имя тега анимации; неизвестная ссылка — null.</param>
/// <param name="Bit">Номер бита в Single bit; null — Range.</param>
/// <param name="Ranges">Диапазоны или состояния бита в порядке файла; null — настройки не прочитаны.</param>
public sealed record AppearanceAnimation(string? Tag, int? Bit, IReadOnlyList<AppearanceRange>? Ranges);
