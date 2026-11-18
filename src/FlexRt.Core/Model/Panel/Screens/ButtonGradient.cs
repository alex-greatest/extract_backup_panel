namespace FlexRt.Model.Panel.Screens;

/// <summary>Переход градиента из PS_BorderedShap; ширина 0 не различает выключение и нулевой отступ.</summary>
/// <param name="Vertical">Вертикальное направление; иначе горизонтальное.</param>
/// <param name="Width">Скомпилированная ширина перехода.</param>
/// <param name="ColorRgb">Крайний цвет перехода <c>0xRRGGBB</c>.</param>
public sealed record ButtonGradient(bool Vertical, int Width, int ColorRgb);
