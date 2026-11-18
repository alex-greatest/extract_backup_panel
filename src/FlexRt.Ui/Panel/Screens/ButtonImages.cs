using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Pictures;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Извлечённые картинки подписей и оформления кнопок с отложенным созданием адресов для холста.</summary>
public sealed class ButtonImages
{
    /// <summary>Картинки по ссылкам TOC открытой панели.</summary>
    private readonly Dictionary<VarReference, PanelPicture> _pictures;

    /// <summary>Картинки оформления по номеру записи TILED_PICTURE.</summary>
    private readonly Dictionary<int, PanelPicture> _frames;

    /// <summary>Адреса уже показанных картинок; байты не преобразуются заново при выборе объекта.</summary>
    private readonly Dictionary<PanelPicture, string> _urls = new();

    /// <summary>Собрать ссылки после завершения чтения картинок панели.</summary>
    public ButtonImages(FwxDocument document)
    {
        _pictures = document.Pictures.Where(picture => document.FindTable(picture.Table) is not null)
            .ToDictionary(picture => new VarReference(document.FindTable(picture.Table)!.Id, picture.Index));
        _frames = document.Pictures.Where(picture => picture.Table == "TILED_PICTURE")
            .ToDictionary(picture => picture.Index);
    }

    /// <summary>Адрес картинки ненажатой подписи из PICTURE.</summary>
    /// <returns>Адрес; ссылка или картинка отсутствует — null.</returns>
    public string? Caption(ButtonCaption caption) => caption.Picture is { } link
        && _pictures.TryGetValue(link, out var picture) ? Url(picture) : null;

    /// <summary>Адрес исходной картинки рамки из TILED_PICTURE.</summary>
    /// <returns>Адрес; картинка не прочитана — null.</returns>
    public string? Frame(ButtonFrame frame) => _frames.TryGetValue(frame.PictureIndex, out var picture)
        ? Url(picture) : null;

    /// <summary>Создать адрес PNG или JPEG один раз на открытую картинку.</summary>
    /// <returns>Адрес для CSS и элемента img.</returns>
    private string Url(PanelPicture picture)
    {
        if (_urls.TryGetValue(picture, out var url))
        {
            return url;
        }
        var mime = picture.Extension == "jpg" ? "jpeg" : picture.Extension;
        url = $"data:image/{mime};base64,{Convert.ToBase64String(picture.Data)}";
        _urls.Add(picture, url);
        return url;
    }
}
