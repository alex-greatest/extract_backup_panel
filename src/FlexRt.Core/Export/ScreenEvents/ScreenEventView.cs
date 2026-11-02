namespace FlexRt.Export.ScreenEvents;

/// <summary>Событие экрана для вкладки Events окна.</summary>
/// <param name="Name">Имя события, как в TIA: <c>Loaded</c> или <c>Cleared</c>.</param>
/// <param name="IsRead">Список функций прочитан; <c>false</c> — сбой FUNC_LIST или ADAPTDEMULTIPLE, функций нет.</param>
/// <param name="Functions">Функции в порядке TIA; пусто — у события нет функций (или оно не прочитано).</param>
public sealed record ScreenEventView(string Name, bool IsRead, IReadOnlyList<FunctionView> Functions);
