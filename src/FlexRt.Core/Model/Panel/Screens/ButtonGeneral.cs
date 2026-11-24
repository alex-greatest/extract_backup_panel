namespace FlexRt.Model.Panel.Screens;

/// <summary>Подтверждённые свойства кнопки; имя и положение находятся у ScreenObject.</summary>
/// <param name="Element">Элемент VIEW_BUTTONS или VIEW_STATEBUTTO.</param>
/// <param name="Mode">Режим кнопки.</param>
/// <param name="Pressed">Отдельная подпись нажатого состояния; неизвестно или неприменимо — null.</param>
/// <param name="Off">Подпись ненажатого состояния.</param>
/// <param name="On">Подпись нажатого состояния.</param>
/// <param name="List">Список подписей; списка нет — null, неизвестная раскладка — Entries null.</param>
public sealed record ButtonGeneral(
    VarReference Element,
    ButtonMode Mode,
    bool? Pressed,
    ButtonCaption Off,
    ButtonCaption On,
    ButtonCaptionList? List)
{
    /// <summary>Оформление обоих состояний; неподтверждённая раскладка или невидимая кнопка — null.</summary>
    public ButtonAppearance? Appearance { get; init; }

    /// <summary>Анимация цветов и мигания; ссылки на анимацию нет — null.</summary>
    public AppearanceAnimation? AnimatedAppearance { get; init; }

    /// <summary>Условия Visibility и Control enable из обёрток кнопки.</summary>
    public IReadOnlyList<FieldCondition> Conditions { get; init; } = [];

    /// <summary>Движения из обёрток кнопки, от внутренней к внешней.</summary>
    public IReadOnlyList<FieldMovement> Movements { get; init; } = [];

    /// <summary>Растягивать графическую подпись; неизвестный код или невидимая кнопка — null.</summary>
    public bool? StretchPicture { get; init; }

    /// <summary>Разрешено ли управление оператору; неизвестно — null.</summary>
    public bool? AllowOperatorControl { get; init; }

    /// <summary>Номер права Authorization; ссылки на право нет — null.</summary>
    public long? Authorization { get; init; }

    /// <summary>Ссылки на Authorization проверены, в том числе когда ссылки на право нет.</summary>
    public bool AuthorizationRead { get; init; }

    /// <summary>Flashing Standard; не прочитано — null.</summary>
    public bool? Flashing { get; init; }
}
