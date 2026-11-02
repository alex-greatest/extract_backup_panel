namespace FlexRt.Model.Panel.Functions;

/// <summary>Вызов системной функции — элемент таблицы FUNC_* (FUNC_BIT, FUNC_ASSIGN, FUNC_COMMAND, …).</summary>
/// <param name="Element">Таблица (Id из TOC) и номер элемента с нуля.</param>
/// <param name="Table">Имя таблицы из TOC; таблицы с таким Id нет — пусто.</param>
/// <param name="Subcode">Подкод: какая функция таблицы (у FUNC_BIT 1 — SetBit, 0 — ResetBit); <c>null</c> — не прочитан.</param>
/// <param name="Names">
/// Имена функции в TIA: одно; два и больше — функции с одинаковыми байтами (HTMLBrowserPageUp и ScreenObjectPageUp);
/// пусто — функция не опознана (таблицы и подкода нет в словаре).
/// </param>
/// <param name="ParameterNames">Имена параметров в TIA по порядку; пустое имя или имени нет — не известно.</param>
/// <param name="Arguments">Значения параметров по порядку; <c>null</c> — элемент не прочитан (предупреждение в <see cref="FwxDocument.Warnings"/>).</param>
public sealed record FunctionCall(
    VarReference Element,
    string Table,
    long? Subcode,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> ParameterNames,
    IReadOnlyList<FunctionArgument>? Arguments);
