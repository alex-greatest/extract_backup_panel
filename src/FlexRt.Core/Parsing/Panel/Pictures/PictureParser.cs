using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Pictures;

/// <summary>Разбор PICTURE: картинки панели (графика экранов и значки), без привязки к объектам.</summary>
public static class PictureParser
{
    /// <summary>
    /// Смещение u32 длины картинки в элементе. Перед ней u32 1, u32 0, u16 1, u16 1 — не читаются
    /// (наблюдение: одинаковы у всех элементов в известных файлах).
    /// </summary>
    private const int LengthOffset = 0x0c;

    /// <summary>
    /// Добавить картинку каждого элемента PICTURE в <see cref="FwxDocument.Pictures"/> в порядке каталога.
    /// Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или картинка не сходится с раскладкой; картинки до сбоя остаются.</exception>
    public static void Parse(FwxDocument doc) => PictureData.ParseTable(doc, "PICTURE", LengthOffset);
}
