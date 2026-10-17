using FlexRt.Export;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Экран для окна: запись SCREEN, вид и значения свойств — те же, что в строке листа «Экраны».</summary>
/// <param name="Screen">Запись SCREEN: размер для холста.</param>
/// <param name="Name">Имя, как на листе «Экраны» (у Permanent area — <c>Permanent area</c>).</param>
/// <param name="Kind">Вид экрана, как в колонке Вид листа: обычно одно из значений <see cref="ScreenKind"/>, иначе <c>?</c> или новый вид ядра.</param>
/// <param name="IsStart">Стартовый экран.</param>
/// <param name="Properties">Свойства для панели Properties: заголовок колонки листа и значение ячейки.</param>
public sealed record ScreenItem(Screen Screen, string Name, string Kind, bool IsStart, IReadOnlyList<(string Name, ReportCell Value)> Properties);
