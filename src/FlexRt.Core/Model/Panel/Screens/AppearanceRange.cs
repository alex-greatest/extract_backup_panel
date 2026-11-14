namespace FlexRt.Model.Panel.Screens;

/// <summary>Диапазон Appearance или состояние бита с цветами и кодом мигания из SYMB.</summary>
/// <param name="From">Начало диапазона; в режиме Single bit — 0 или 1.</param>
/// <param name="To">Конец диапазона.</param>
/// <param name="ForegroundRgb">Цвет текста 0xRRGGBB.</param>
/// <param name="BackgroundRgb">Цвет фона 0xRRGGBB.</param>
/// <param name="Flashing">Код мигания: 0 No, 1 Slow, 2 Medium, 3 Fast; другие значения не установлены.</param>
public sealed record AppearanceRange(long From, long To, int ForegroundRgb, int BackgroundRgb, int Flashing);
