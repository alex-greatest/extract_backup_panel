namespace FlexRt.Model.Panel.Screens;

/// <summary>Скомпилированное оформление обычного и нажатого состояний и фокуса кнопки.</summary>
/// <param name="Normal">Оформление обычного состояния.</param>
/// <param name="Pressed">Оформление нажатого состояния.</param>
/// <param name="Fonts">Прочитанные шрифты языков Runtime; при ошибке — пустой список.</param>
public sealed record ButtonAppearance(
    ButtonStateAppearance Normal,
    ButtonStateAppearance Pressed,
    IReadOnlyList<LanguageFont> Fonts)
{
    /// <summary>Оформление фокуса; отсутствующий или непрочитанный стиль — null.</summary>
    public ButtonFocus? Focus { get; init; }
}
