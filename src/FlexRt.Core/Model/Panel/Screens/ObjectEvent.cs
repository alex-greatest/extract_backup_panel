using FlexRt.Model.Panel.Functions;

namespace FlexRt.Model.Panel.Screens;

/// <summary>Событие объекта экрана и его список системных функций, как на вкладке Events объекта в TIA.</summary>
/// <param name="Kind">Событие.</param>
/// <param name="Functions">
/// Функции в порядке TIA; <c>null</c> — список не прочитан (сбой ADAPTDEMULTIPLE, SECURITY_MONITO или FUNC_LIST,
/// предупреждение в <see cref="FwxDocument.Warnings"/>).
/// </param>
public sealed record ObjectEvent(ObjectEventKind Kind, IReadOnlyList<FunctionCall>? Functions);
