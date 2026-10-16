using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Pictures;

/// <summary>Разбор TILED_PICTURE: картинки оформления (фоны кнопок и рамок), без привязки к объектам.</summary>
public static class TiledPictureParser
{
    /// <summary>
    /// Смещение u32 длины картинки в элементе. Перед ней не читаются: на +0x10 u16 ширина и +0x12
    /// u16 высота картинки (совпали с PNG), на +0x14 четыре u16 — гипотеза: поля растягивания.
    /// </summary>
    private const int LengthOffset = 0x1c;

    /// <summary>
    /// Добавить картинку каждого элемента TILED_PICTURE в <see cref="FwxDocument.Pictures"/> в порядке
    /// каталога. Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или картинка не сходится с раскладкой; картинки до сбоя остаются.</exception>
    public static void Parse(FwxDocument doc) => PictureData.ParseTable(doc, "TILED_PICTURE", LengthOffset);
}
