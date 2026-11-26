namespace FlexRt.Model.Panel.Functions;

/// <summary>Вызов системной функции или VB-скрипта — элемент таблицы FUNC_*.</summary>
/// <param name="Element">Таблица (Id из TOC) и номер элемента с нуля.</param>
/// <param name="Table">Имя таблицы из TOC; таблицы с таким Id нет — пусто.</param>
/// <param name="Subcode">Подкод системной функции или номер VB-скрипта; <c>null</c> — не прочитан.</param>
/// <param name="Names">
/// Имена функции в TIA: одно; два и больше — функции с одинаковыми байтами (HTMLBrowserPageUp и ScreenObjectPageUp);
/// пусто — системная функция не опознана. Для вызова VB — имя скрипта или подпись с номером.
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
