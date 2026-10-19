namespace FlexRt.Ui.Panel.Pictures;

/// <summary>
/// Адреса картинок открытой панели для браузера окна: <c>img</c> получает короткий адрес, а не <c>data:</c> с байтами, —
/// через мост Blazor идут только адреса, браузер кэширует картинку и откладывает загрузку миниатюр (<c>loading="lazy"</c>).
/// Байты по адресу отдаёт окно (<see cref="MainWindow"/>, событие WebView2 <c>WebResourceRequested</c>) из
/// <see cref="PanelPictures.Find"/>. Хоста нет в сети: WebView2 отвечает на запрос сам, до DNS.
/// </summary>
public static class PictureHost
{
    /// <summary>Начало адреса картинки.</summary>
    public const string Origin = "https://pictures.flexrt";

    /// <summary>Фильтр запросов WebView2: все адреса картинок.</summary>
    public const string Filter = Origin + "/*";

    /// <summary>
    /// Адрес картинки <paramref name="name"/> открытия панели <paramref name="load"/>: номер открытия в адресе не даёт браузеру
    /// показать из кэша одноимённую картинку прежней панели.
    /// </summary>
    /// <returns>Адрес вида <c>https://pictures.flexrt/3/PICTURE_5.png</c>.</returns>
    public static string Url(int load, string name) => $"{Origin}/{load}/{name}";
}
