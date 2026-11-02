namespace FlexRt.Export.ScreenEvents;

/// <summary>Параметр системной функции для вкладки Events окна.</summary>
/// <param name="Name">Имя параметра, как в TIA (<c>Tag</c>); не известно — <c>Параметр k</c>.</param>
/// <param name="Value">Значение текстом: имя тега, число, строка, имя объекта.</param>
public sealed record ParameterView(string Name, string Value);
