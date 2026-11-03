namespace FlexRt.Model.Panel.Screens;

/// <summary>Цвет и выравнивание текста объекта из элемента PS_Text, как Appearance → Text и Text format в TIA.</summary>
/// <param name="ColorRgb">Цвет текста <c>0xRRGGBB</c>.</param>
/// <param name="HorizontalAlignment">Код выравнивания по горизонтали: 0 — Right, 1 — Center, 2 — Left.</param>
/// <param name="VerticalAlignment">Код выравнивания по вертикали: 0 — Top, 1 — Middle, 2 — Bottom.</param>
public sealed record TextStyle(int ColorRgb, int HorizontalAlignment, int VerticalAlignment);
