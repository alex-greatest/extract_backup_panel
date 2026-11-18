namespace FlexRt.Model.Panel.Screens;

/// <summary>Основной цвет и прозрачность заливки кнопки из PS_Fill_Color.</summary>
/// <param name="ColorRgb">Цвет <c>0xRRGGBB</c>; у градиента — его средний цвет.</param>
/// <param name="Transparent">Прозрачная заливка.</param>
public sealed record ButtonFill(int ColorRgb, bool Transparent);
