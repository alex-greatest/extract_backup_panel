namespace FlexRt.Ui.Panel.Saving;

/// <summary>Пункты сохранения панели инструментов окна: что сохранить (<see cref="PanelSaver.SaveAsync"/>).</summary>
public static class SaveItem
{
    /// <summary>Вся книга <c>panel_data.xlsx</c>.</summary>
    public const string Book = "book";

    /// <summary>Лист раздела, выбранного в дереве, отдельной книгой.</summary>
    public const string Sheet = "sheet";

    /// <summary>Книга текстов <c>PDATA.xlsx</c>.</summary>
    public const string Strings = "strings";

    /// <summary>Картинка, выбранная в галерее, отдельным файлом.</summary>
    public const string Picture = "picture";

    /// <summary>Все картинки панели архивом <c>images.zip</c>.</summary>
    public const string Pictures = "pictures";
}
