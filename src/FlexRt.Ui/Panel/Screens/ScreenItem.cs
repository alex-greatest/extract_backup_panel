using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Экран для окна: запись SCREEN, вид и значения свойств — те же, что в строке листа «Экраны».</summary>
/// <param name="Screen">Запись SCREEN: ширина и цвет фона для холста.</param>
/// <param name="Name">Имя, как на листе «Экраны» (у Permanent area — <c>Permanent area</c>).</param>
/// <param name="Kind">Вид экрана, как в колонке Вид листа: обычно одно из значений <see cref="ScreenKind"/>, иначе <c>?</c> или новый вид ядра.</param>
/// <param name="IsStart">Стартовый экран.</param>
/// <param name="Properties">Свойства для панели Properties: заголовок колонки листа, значение ячейки и язык колонки.</param>
/// <param name="Height">
/// Высота экрана на холсте — та же, что в колонке Height листа (у Permanent area — её высота из VIEW_DYN, запись
/// <c>Overview</c> — вся панель); не известна (<c>?</c> у Permanent area) — <c>null</c>.
/// </param>
/// <param name="Strip">
/// Полоса Permanent area над экраном: у обычного экрана и экрана неопознанного вида, при высоте Permanent area больше нуля.
/// </param>
public sealed record ScreenItem(Screen Screen, string Name, string Kind, bool IsStart, IReadOnlyList<ScreenProperty> Properties, int? Height, PermanentStrip? Strip);
