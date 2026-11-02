namespace FlexRt.Export.ScreenEvents;

/// <summary>Системная функция события для вкладки Events окна.</summary>
/// <param name="Number">Номер в списке события, с единицы.</param>
/// <param name="Name">
/// Имя, как в TIA (<c>SetBit</c>); функции с одинаковыми байтами — через « / »; не опознана — таблица и подкод
/// (<c>FUNC_SHOWPOPUPS (подкод 0x3)</c>); не прочитана — <see cref="ScreenEventTexts.NotRead"/>.
/// </param>
/// <param name="IsRead">Элемент функции прочитан.</param>
/// <param name="Element">Таблица и номер элемента с единицы (<c>FUNC_BIT 4</c>): где лежит функция.</param>
/// <param name="Parameters">Параметры по порядку; не прочитана — пусто.</param>
public sealed record FunctionView(int Number, string Name, bool IsRead, string Element, IReadOnlyList<ParameterView> Parameters);
