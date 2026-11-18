using FlexRt.Export.Pictures;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Export.ScreenObjects;

/// <summary>Тексты General кнопки: язык панели, управляющие теги и имена файлов извлечённых картинок.</summary>
public sealed class ButtonTexts
{
    /// <summary>Строки панели по языку и номеру.</summary>
    private readonly LanguageStrings _strings;

    /// <summary>Имена файлов картинок по ссылке из General.</summary>
    private readonly Dictionary<VarReference, string> _pictures;

    /// <summary>Имена прямых тегов VAR по ссылке из SYMB.</summary>
    private readonly Dictionary<VarReference, string> _tags;

    /// <summary>Собрать данные отображения один раз на панель после завершения разбора.</summary>
    public ButtonTexts(FwxDocument doc)
    {
        _strings = LanguageStrings.Of(doc);
        _pictures = new Dictionary<VarReference, string>();
        foreach (var picture in doc.Pictures)
        {
            if (doc.FindTable(picture.Table) is { } table)
            {
                _pictures.Add(new VarReference(table.Id, picture.Index), PictureArchiveExporter.EntryName(picture));
            }
        }
        var vars = doc.FindTable("VAR");
        _tags = vars is null ? [] : doc.Tags.ToDictionary(t => new VarReference(vars.Id, t.Index - 1), t => t.Name);
    }

    /// <summary>Режим Mode как в TIA; списки выбираются внутри Text или Graphic.</summary>
    /// <returns>Имя режима.</returns>
    public static string Mode(ButtonMode mode) => mode switch
    {
        ButtonMode.Text or ButtonMode.TextList => "Text",
        ButtonMode.Graphic or ButtonMode.GraphicList => "Graphic",
        ButtonMode.GraphicAndText => "Graphics and text",
        ButtonMode.Invisible => "Invisible",
        _ => "Не разобрано"
    };

    /// <summary>Текст подписи на выбранном языке; «Все языки» или отсутствующий язык — первый язык панели.</summary>
    /// <returns>Текст как в STRINGSTORE; текстовой подписи нет — пусто.</returns>
    public string Text(ButtonCaption caption, int? language)
    {
        var selected = LanguageChoice.Kept(_strings.Languages, language) ?? LanguageChoice.First(_strings.Languages);
        return caption.TextIndex is { } index && selected is { } lcid ? _strings.Text(lcid, index) : "";
    }

    /// <summary>Имя файла картинки из General — то же, что в галерее и images.zip.</summary>
    /// <returns>Имя файла; ссылка есть, а картинка не прочитана — сообщение; ссылки нет — пусто.</returns>
    public string Picture(ButtonCaption caption) => caption.Picture is { } link
        ? _pictures.GetValueOrDefault(link, "не прочитано")
        : "";

    /// <summary>Имя прямого управляющего тега списка.</summary>
    /// <returns>Имя; неизвестная связь — сообщение, списка нет — пусто.</returns>
    public string Tag(ButtonCaptionList? list) => list is null ? "" : list.Tag is { } link
        ? _tags.GetValueOrDefault(link, "не прочитано")
        : "Не разобрано";
}
