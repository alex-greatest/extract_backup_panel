namespace FlexRt.Model.Panel.Screens;

/// <summary>Диапазон значений и подпись кнопки из SYMB.</summary>
/// <param name="From">Нижняя граница включительно.</param>
/// <param name="To">Верхняя граница включительно.</param>
/// <param name="Caption">Текст или картинка диапазона.</param>
public sealed record ButtonListEntry(long From, long To, ButtonCaption Caption);
