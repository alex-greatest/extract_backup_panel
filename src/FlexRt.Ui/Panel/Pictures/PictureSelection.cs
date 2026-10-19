namespace FlexRt.Ui.Panel.Pictures;

/// <summary>
/// Картинки открытой панели и выбор в узле «Картинки»: выбранная картинка и открыта ли она на всё окно. Любая смена
/// панели или узла дерева закрывает просмотр на всё окно, чтобы он не открылся сам при следующем выборе. Служба окна, одна
/// на окно (<see cref="MainWindow"/>): её картинками окно отвечает браузеру на адреса картинок.
/// </summary>
public sealed class PictureSelection
{
    /// <summary>Картинки открытой панели; панель не открыта — пусто.</summary>
    public PanelPictures Pictures { get; private set; } = PanelPictures.None;

    /// <summary>Выбранная картинка; узел «Картинки» не выбран — <c>null</c>.</summary>
    public PictureItem? Selected { get; private set; }

    /// <summary>Картинка, открытая на всё окно; просмотр закрыт — <c>null</c>.</summary>
    public PictureItem? Shown => _fullscreen ? Selected : null;

    /// <summary>Выбранная картинка открыта на всё окно.</summary>
    private bool _fullscreen;

    /// <summary>Картинки новой панели (<see cref="PanelPictures.None"/> — панель убрана); выбор и просмотр сбрасываются.</summary>
    public void Load(PanelPictures pictures)
    {
        Pictures = pictures;
        SelectNode(false);
    }

    /// <summary>В дереве выбран узел: «Картинки» — выбрана первая картинка, другой — ни одной; просмотр закрыт.</summary>
    public void SelectNode(bool isPictures)
    {
        Selected = isPictures && Pictures.Items.Count > 0 ? Pictures.Items[0] : null;
        _fullscreen = false;
    }

    /// <summary>Выбрать картинку: щелчок по плитке или листание в просмотре на всё окно.</summary>
    public void Select(PictureItem picture) => Selected = picture;

    /// <summary>Выбрать картинку и открыть её на всё окно.</summary>
    public void Open(PictureItem picture) => (Selected, _fullscreen) = (picture, true);

    /// <summary>Закрыть просмотр на всё окно; выбор остаётся.</summary>
    public void Close() => _fullscreen = false;
}
