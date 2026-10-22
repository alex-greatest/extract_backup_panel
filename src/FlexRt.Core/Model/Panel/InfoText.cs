namespace FlexRt.Model.Panel;

/// <summary>
/// Текст-подсказка элемента другой таблицы (Tooltip экрана или объекта, Info text аларма в TIA): пара из INFO_HANDLER.
/// </summary>
/// <param name="Owner">
/// Чей текст: <see cref="VarReference.Table"/> — Id таблицы (наблюдение: 7 — SCREEN, 2 — VIEW_IN, 0x17 — VIEW_BUTTONS,
/// 0x1C — MSG_NUMEVENT), <see cref="VarReference.Index"/> — номер элемента с нуля (у SCREEN — <see cref="Screens.Screen.Index"/>).
/// </param>
/// <param name="TextIndex">
/// Номер строки языков (<see cref="LangString.Idx"/>); на каждый язык Runtime своя строка с этим номером. Текст — как
/// введён в TIA, перенос строки — CRLF.
/// </param>
public readonly record struct InfoText(VarReference Owner, int TextIndex);
