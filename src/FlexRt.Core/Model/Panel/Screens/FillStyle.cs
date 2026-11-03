namespace FlexRt.Model.Panel.Screens;

/// <summary>Заливка объекта из элемента PS_Fill_Color, как Appearance → Background → Color в TIA.</summary>
/// <param name="ColorRgb">Цвет фона <c>0xRRGGBB</c>.</param>
public sealed record FillStyle(int ColorRgb);
