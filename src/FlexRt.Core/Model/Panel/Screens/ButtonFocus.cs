namespace FlexRt.Model.Panel.Screens;

/// <summary>Оформление фокуса кнопки из PS_Focus.</summary>
/// <param name="Width">Ширина рамки фокуса в пикселях.</param>
/// <param name="ColorRgb">Цвет рамки фокуса <c>0xRRGGBB</c>.</param>
public sealed record ButtonFocus(int Width, int ColorRgb);
