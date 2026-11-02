using FlexRt.Model.Panel.Functions;

namespace FlexRt.Model.Panel.Screens;

/// <summary>Событие экрана и его список системных функций, как на вкладке Events экрана в TIA.</summary>
/// <param name="ScreenIndex">Номер записи SCREEN экрана, с нуля.</param>
/// <param name="Kind">Loaded или Cleared.</param>
/// <param name="Functions">
/// Функции в порядке TIA; <c>null</c> — список не прочитан (сбой FUNC_LIST или ADAPTDEMULTIPLE, предупреждение в
/// <see cref="FwxDocument.Warnings"/>).
/// </param>
public sealed record ScreenEvent(int ScreenIndex, ScreenEventKind Kind, IReadOnlyList<FunctionCall>? Functions);
