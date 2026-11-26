namespace FlexRt.Model.Panel.Screens;

/// <summary>
/// Событие объекта на вкладке Events в TIA, имя как в Openness.
/// </summary>
public enum ObjectEventKind
{
    /// <summary>Activate: ссылка поля на ADAPTDEMULTIPLE со значением 1 (шаги 82, 85).</summary>
    Activate,

    /// <summary>Deactivate: ссылка поля на ADAPTDEMULTIPLE со значением 0 (шаги 83, 85).</summary>
    Deactivate,

    /// <summary>OnFinishInput: ссылка поля со словом <c>0x8141</c> — на функцию, FUNC_LIST или SECURITY_MONITO (шаги 84, 85, A603A0097).</summary>
    OnFinishInput,

    /// <summary>Щелчок по кнопке.</summary>
    Click,

    /// <summary>Нажатие кнопки.</summary>
    Press,

    /// <summary>Отпускание кнопки.</summary>
    Release,

    /// <summary>Изменение состояния кнопки.</summary>
    Change
}
