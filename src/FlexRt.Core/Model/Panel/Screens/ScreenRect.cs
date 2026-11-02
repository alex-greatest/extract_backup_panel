namespace FlexRt.Model.Panel.Screens;

/// <summary>Положение и размер объекта экрана в пикселях, как Layout → Position &amp; size в TIA.</summary>
/// <param name="Left">X — левый край от левого края экрана.</param>
/// <param name="Top">Y — верхний край от верхнего края экрана.</param>
/// <param name="Width">Ширина; у горизонтальной линии — 0.</param>
/// <param name="Height">Высота; у вертикальной линии — 0.</param>
public readonly record struct ScreenRect(int Left, int Top, int Width, int Height);
