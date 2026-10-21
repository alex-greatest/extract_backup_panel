using System.Buffers.Binary;
using FlexRt.Model.Panel.Pictures;

namespace FlexRt.Parsing.Panel.Pictures;

/// <summary>
/// Ширина и высота картинки из заголовка её файла PNG или JPEG (<see cref="PanelPicture.Data"/>) —
/// для свойств картинки в окне.
/// </summary>
public static class PictureSize
{
    /// <summary>Смещение ширины в PNG: сигнатура 8 байт, длина и тип блока <c>IHDR</c> по 4 байта.</summary>
    private const int PngWidthOffset = 0x10;

    /// <summary>
    /// Прочитать размер: PNG — из блока <c>IHDR</c> (u32 ширина и высота, big-endian), JPEG — из первого маркера
    /// <c>SOFn</c> (u16 высота, затем ширина, big-endian); другой формат — размера нет. Исключений не бросает.
    /// </summary>
    /// <param name="data">Байты файла картинки.</param>
    /// <param name="extension">Формат по сигнатуре: <see cref="PictureFormat.Png"/> или <see cref="PictureFormat.Jpg"/>.</param>
    /// <returns>Ширина и высота; заголовок не прочитан или формат другой — <c>null</c>.</returns>
    public static (int Width, int Height)? Read(byte[] data, string extension) => extension switch
    {
        PictureFormat.Png => Png(data),
        PictureFormat.Jpg => Jpeg(data),
        _ => null
    };

    /// <summary>Размер из блока <c>IHDR</c>, который в PNG всегда первый.</summary>
    /// <returns>Ширина и высота; файл короче блока — <c>null</c>.</returns>
    private static (int Width, int Height)? Png(byte[] data)
    {
        if (data.Length < PngWidthOffset + 8)
        {
            return null;
        }
        return ((int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(PngWidthOffset)),
            (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(PngWidthOffset + 4)));
    }

    /// <summary>
    /// Размер из первого маркера <c>SOFn</c> (0xC0–0xCF, кроме 0xC4 DHT, 0xC8 JPG, 0xCC DAC): сегменты после SOI
    /// пропускаются по их длине; байты заполнения 0xFF перед маркером и маркеры без длины (TEM 0x01, RSTn 0xD0–0xD7)
    /// пропускаются.
    /// </summary>
    /// <returns>Ширина и высота; маркер не найден до конца файла или до SOS — <c>null</c>.</returns>
    private static (int Width, int Height)? Jpeg(byte[] data)
    {
        var p = 2;
        while (p + 2 <= data.Length && data[p] == 0xFF)
        {
            var marker = data[p + 1];
            switch (marker)
            {
                case 0xFF:
                    p++;
                    continue;
                case 0x01 or >= 0xD0 and <= 0xD7:
                    p += 2;
                    continue;
            }
            if (p + 4 > data.Length)
            {
                return null;
            }
            var length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(p + 2));
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return p + 9 <= data.Length
                    ? (BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(p + 7)), BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(p + 5)))
                    : null;
            }
            if (marker == 0xDA)
            {
                return null;
            }
            p += 2 + length;
        }
        return null;
    }
}
