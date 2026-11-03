namespace FlexRt.Model.Panel.Screens;

/// <summary>Рамка и углы объекта из элемента PS_BorderedShap, как Appearance → Border и Corner radius в TIA.</summary>
/// <param name="EdgeStyle">Код стиля рамки (Style): 1 — Solid, 2 — Double line; другие коды не проверены.</param>
/// <param name="Width">Ширина рамки в пикселях (Width).</param>
/// <param name="ColorRgb">Цвет рамки <c>0xRRGGBB</c> (Color).</param>
/// <param name="BackColorRgb">Цвет фона рамки <c>0xRRGGBB</c> (Background color).</param>
/// <param name="CornerRadius">Радиус углов в пикселях (Corner radius; в файле — по углу, читается первый).</param>
public sealed record BorderStyle(int EdgeStyle, int Width, int ColorRgb, int BackColorRgb, int CornerRadius);
