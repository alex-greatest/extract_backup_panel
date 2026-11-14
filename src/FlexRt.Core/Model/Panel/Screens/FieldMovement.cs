namespace FlexRt.Model.Panel.Screens;

/// <summary>Движение поля: прямая связь координат с тегами либо интерполяция между точками по диапазону тега.</summary>
/// <param name="Direct">Прямые связи координат; false — движение по диапазону.</param>
/// <param name="TagX">Тег X у Direct либо общий тег диапазона.</param>
/// <param name="TagY">Тег Y у Direct.</param>
/// <param name="From">Начало диапазона тега.</param>
/// <param name="To">Конец диапазона тега.</param>
/// <param name="StartX">Начальная координата X.</param>
/// <param name="StartY">Начальная координата Y.</param>
/// <param name="EndX">Конечная координата X.</param>
/// <param name="EndY">Конечная координата Y.</param>
/// <param name="IsRead">Настройки прочитаны; false — ошибка формата.</param>
public sealed record FieldMovement(bool Direct, string? TagX, string? TagY, double? From, double? To,
    int? StartX, int? StartY, int? EndX, int? EndY, bool IsRead = true);
