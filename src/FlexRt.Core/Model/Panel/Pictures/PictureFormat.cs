namespace FlexRt.Model.Panel.Pictures;

/// <summary>Форматы картинок панели — значения <see cref="PanelPicture.Extension"/>, они же расширения файлов.</summary>
public static class PictureFormat
{
    /// <summary>PNG: сигнатура <c>89 50 4E 47 0D 0A 1A 0A</c>.</summary>
    public const string Png = "png";

    /// <summary>JPEG: сигнатура <c>FF D8 FF</c>.</summary>
    public const string Jpg = "jpg";
}
