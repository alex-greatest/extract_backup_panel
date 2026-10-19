using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Pictures;

namespace FlexRt.Parsing.Panel.Pictures;

/// <summary>
/// Данные картинки в элементе PICTURE и TILED_PICTURE: u32 длина в байтах, сразу за ней файл PNG
/// или JPEG целиком, затем нули до кратности 4 (наблюдение на samples/pdata.fwc, A603A0097 и
/// CCB-A13: у всех 233 картинок длина совпала с концом файла по IEND или FFD9).
/// </summary>
internal static class PictureData
{
    /// <summary>Сигнатура PNG.</summary>
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    /// <summary>Сигнатура JPEG (маркер SOI и начало следующего маркера).</summary>
    private static readonly byte[] JpegSignature = [0xff, 0xd8, 0xff];

    /// <summary>
    /// Обойти элементы таблицы и добавить в <see cref="FwxDocument.Pictures"/> картинку каждого элемента
    /// сразу после чтения. Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или картинка не сходится с раскладкой.</exception>
    public static void ParseTable(FwxDocument doc, string tableName, int lengthOffset)
    {
        var table = doc.FindTable(tableName);
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            doc.Pictures.Add(ReadRecord(doc.Binary, table.Name, item, lengthOffset));
        }
    }

    /// <summary>Прочитать картинку элемента: длина на <paramref name="lengthOffset"/>, данные за ней.</summary>
    /// <returns>Картинка с расширением по сигнатуре.</returns>
    /// <exception cref="FwxFormatException">
    /// Длина не помещается в элемент, равна нулю или данные выходят за элемент; сигнатура не PNG и не JPEG.
    /// </exception>
    private static PanelPicture ReadRecord(FwxBinary b, string tableName, TableItem item, int lengthOffset)
    {
        if (lengthOffset + 4 > item.Length)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {item.Index + 1}: длина картинки не помещается в {item.Length} байт");
        }
        var length = b.D4(item.Offset + lengthOffset);
        var start = lengthOffset + 4;
        if (length == 0 || length > item.Length - start)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {item.Index + 1}: картинка {length} байт не помещается в {item.Length} байт");
        }

        var data = b.Span(item.Offset + start, length);
        return new PanelPicture(tableName, item.Index, Extension(b, item, start, data), data.ToArray());
    }

    /// <summary>Расширение файла картинки по сигнатуре данных.</summary>
    /// <returns><see cref="PictureFormat.Png"/> или <see cref="PictureFormat.Jpg"/>.</returns>
    /// <exception cref="FwxFormatException">Сигнатура не PNG и не JPEG.</exception>
    private static string Extension(FwxBinary b, TableItem item, int start, ReadOnlySpan<byte> data)
    {
        if (data.StartsWith(PngSignature))
        {
            return PictureFormat.Png;
        }
        if (data.StartsWith(JpegSignature))
        {
            return PictureFormat.Jpg;
        }
        throw new FwxFormatException(b.Section, item.Offset + start, $"запись {item.Index + 1}: незнакомый формат картинки");
    }
}
