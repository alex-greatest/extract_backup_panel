namespace FlexRt.Model.Panel.Screens;

/// <summary>Поля картинки кнопки в пикселях, как Layout → Picture margins.</summary>
/// <param name="Top">Сверху.</param>
/// <param name="Bottom">Снизу.</param>
/// <param name="Left">Слева.</param>
/// <param name="Right">Справа.</param>
public sealed record ButtonPictureMargins(int Top, int Bottom, int Left, int Right);
