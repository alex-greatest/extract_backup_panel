namespace FlexRt.Model.Panel.Screens;

/// <summary>
/// Цвета Limits I/O field (Above upper limit, Below lower limit) из элемента ADAPTVALUECHANG поля. TIA пишет их, только
/// если у тега задан Range или тег идёт через VARLIST; иначе у поля их нет (<see cref="IoField.Limits"/> — <c>null</c>).
/// </summary>
/// <param name="AboveUpperRgb">Цвет выше верхнего предела <c>0xRRGGBB</c>; не прочитан — <c>null</c>.</param>
/// <param name="BelowLowerRgb">Цвет ниже нижнего предела <c>0xRRGGBB</c>; не прочитан — <c>null</c>.</param>
public sealed record LimitColors(int? AboveUpperRgb, int? BelowLowerRgb);
