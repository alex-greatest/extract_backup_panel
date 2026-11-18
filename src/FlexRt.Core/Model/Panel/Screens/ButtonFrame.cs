namespace FlexRt.Model.Panel.Screens;

/// <summary>Картинка оформления кнопки из TILED_PICTURE и её границы растягивания.</summary>
/// <param name="PictureIndex">Номер картинки TILED_PICTURE с нуля.</param>
/// <param name="Width">Ширина исходной картинки.</param>
/// <param name="Height">Высота исходной картинки.</param>
/// <param name="Left">Левая граница растягивания.</param>
/// <param name="Top">Верхняя граница растягивания.</param>
/// <param name="Right">Правая граница растягивания.</param>
/// <param name="Bottom">Нижняя граница растягивания.</param>
public sealed record ButtonFrame(int PictureIndex, int Width, int Height, int Left, int Top, int Right, int Bottom);
