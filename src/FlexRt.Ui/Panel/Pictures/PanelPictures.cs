using FlexRt.Export.Pictures;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel.Pictures;

namespace FlexRt.Ui.Panel.Pictures;

/// <summary>Узел «Картинки» дерева проекта: все картинки панели в порядке ядра (сначала <c>PICTURE</c>, затем <c>TILED_PICTURE</c>).</summary>
/// <param name="Items">Картинки; нет — пусто, узла в дереве тогда нет.</param>
public sealed record PanelPictures(IReadOnlyList<PictureItem> Items)
{
    /// <summary>Имя узла в дереве, в пути заголовка галереи и в заголовке свойств картинки.</summary>
    public const string Title = "Картинки";

    /// <summary>Картинок нет: панель не открыта или в ней нет картинок.</summary>
    public static readonly PanelPictures None = new([]);

    /// <summary>Сколько раз строились картинки панели: номер открытия в адресе картинки (<see cref="PictureHost.Url"/>).</summary>
    private static int _loads;

    /// <summary>
    /// Картинки <see cref="FwxDocument.Pictures"/> — под именами записей архива
    /// (<see cref="PictureArchiveExporter.EntryName"/>), с размером из заголовка файла и адресом с новым номером открытия.
    /// </summary>
    /// <returns>Узел «Картинки»; картинок нет — пустой.</returns>
    public static PanelPictures From(FwxDocument doc)
    {
        var load = Interlocked.Increment(ref _loads);
        return new PanelPictures([.. doc.Pictures.Select(p => new PictureItem(p, PictureSize.Read(p.Data, p.Extension), load))]);
    }

    /// <summary>Картинка по адресу <see cref="PictureItem.Source"/> — для ответа браузеру окна.</summary>
    /// <returns>Картинка; адрес не этой панели — <c>null</c>.</returns>
    public PictureItem? Find(string url) => Items.FirstOrDefault(p => p.Source == url);
}
