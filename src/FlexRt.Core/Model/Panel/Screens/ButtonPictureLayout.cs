namespace FlexRt.Model.Panel.Screens;

/// <summary>Положение картинки кнопки из PS_Position, как Layout в TIA.</summary>
/// <param name="Margins">Поля картинки.</param>
/// <param name="HorizontalAlignment">Код: 0 — Right, 1 — Center, 2 — Left.</param>
/// <param name="VerticalAlignment">Код: 0 — Top, 1 — Middle, 2 — Bottom.</param>
public sealed record ButtonPictureLayout(
    ButtonPictureMargins Margins,
    int HorizontalAlignment,
    int VerticalAlignment);
