using FlexRt.Export;
using FlexRt.Export.Pictures;
using FlexRt.Model.Panel.Pictures;
using FlexRt.Parsing.Panel.Pictures;

namespace FlexRt.Ui.Panel.Pictures;

/// <summary>Картинка панели для окна: данные из ядра, имя файла и размер из заголовка файла.</summary>
/// <param name="Picture">Картинка из <c>PICTURE</c> или <c>TILED_PICTURE</c>; её байты нужны и для показа, и для сохранения.</param>
/// <param name="Size">Ширина и высота из заголовка файла (<see cref="PictureSize.Read"/>); не прочитаны — <c>null</c>.</param>
/// <param name="Load">Номер открытия панели — часть адреса картинки (<see cref="PictureHost.Url"/>).</param>
public sealed record PictureItem(PanelPicture Picture, (int Width, int Height)? Size, int Load)
{
    /// <summary>Значение свойства, которое из байтов не прочитано.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>Имя файла картинки, как в архиве картинок (<see cref="PictureArchiveExporter.EntryName"/>): <c>PICTURE_5.png</c>.</summary>
    public string Name => PictureArchiveExporter.EntryName(Picture);

    /// <summary>Адрес картинки для тега <c>img</c> (<see cref="PictureHost"/>): байты отдаёт окно, на диск они не пишутся.</summary>
    public string Source => PictureHost.Url(Load, Name);

    /// <summary>Тип содержимого для ответа браузеру: по формату картинки.</summary>
    public string MediaType => Picture.Extension switch
    {
        PictureFormat.Png => "image/png",
        PictureFormat.Jpg => "image/jpeg",
        _ => "application/octet-stream"
    };

    /// <summary>
    /// Свойства для панели Properties: имя файла, таблица, номер элемента с единицы (как в имени файла), формат, размер
    /// файла в байтах, ширина и высота (не прочитаны — «неизвестно»).
    /// </summary>
    public IReadOnlyList<(string Name, ReportCell Value)> Properties =>
    [
        ("Name", Name),
        ("Table", Picture.Table),
        ("Number", Picture.Index + 1),
        ("Format", Picture.Extension.ToUpperInvariant()),
        ("Bytes", Picture.Data.Length),
        ("Width", Size is { } w ? w.Width : Unknown),
        ("Height", Size is { } h ? h.Height : Unknown)
    ];
}
