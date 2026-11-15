namespace FlexRt.Model.Panel.Screens;

/// <summary>Шрифт объекта для одного языка Runtime из записи FONT.</summary>
/// <param name="Language">LCID языка.</param>
/// <param name="Font">Имя, размер и начертание шрифта.</param>
public sealed record LanguageFont(int Language, PanelFont Font);
