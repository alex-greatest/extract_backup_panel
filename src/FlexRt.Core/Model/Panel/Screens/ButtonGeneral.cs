namespace FlexRt.Model.Panel.Screens;

/// <summary>Подтверждённые свойства General кнопки; имя и положение находятся у ScreenObject.</summary>
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
}
