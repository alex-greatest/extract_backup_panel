namespace FlexRt.Model.Panel.Pictures;

/// <summary>Картинка панели: файл PNG или JPEG, лежащий в элементе таблицы как есть.</summary>
/// <param name="Table">Таблица TOC, в которой лежит картинка (<c>PICTURE</c>, <c>TILED_PICTURE</c>).</param>
/// <param name="Index">Номер элемента в таблице, с нуля.</param>
/// <param name="Extension">Расширение по сигнатуре данных: <c>png</c> или <c>jpg</c>.</param>
/// <param name="Data">Байты файла картинки.</param>
public sealed record PanelPicture(string Table, int Index, string Extension, byte[] Data);
