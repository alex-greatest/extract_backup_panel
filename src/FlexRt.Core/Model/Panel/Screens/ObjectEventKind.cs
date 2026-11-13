namespace FlexRt.Model.Panel.Screens;

/// <summary>
/// Событие I/O field (вкладка Events объекта в TIA), имена — как в Openness. Click TIA принимает, но в файл не пишет
/// (шаг 81); у поля Output события в файл не попадают (шаг 86) — копии <c>data/real/steps-iofield/</c>, V21, TP700.
/// </summary>
public enum ObjectEventKind
{
    /// <summary>Activate: ссылка поля на ADAPTDEMULTIPLE со значением 1 (шаги 82, 85).</summary>
    Activate,

    /// <summary>Deactivate: ссылка поля на ADAPTDEMULTIPLE со значением 0 (шаги 83, 85).</summary>
    Deactivate,

    /// <summary>OnFinishInput: ссылка поля со словом <c>0x8141</c> — на функцию, FUNC_LIST или SECURITY_MONITO (шаги 84, 85, A603A0097).</summary>
    OnFinishInput
}
