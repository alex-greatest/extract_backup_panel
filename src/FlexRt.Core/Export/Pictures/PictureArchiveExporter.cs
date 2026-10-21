using System.IO.Compression;
using FlexRt.Model.Panel.Pictures;

namespace FlexRt.Export.Pictures;

/// <summary>Выгрузка картинок панели одним архивом zip — для окна.</summary>
public static class PictureArchiveExporter
{
    /// <summary>Имя архива по умолчанию (решение пользователя).</summary>
    public const string FileName = "images.zip";

    /// <summary>
    /// Записать архив <paramref name="path"/>: каждая картинка — файл в корне архива под именем
    /// <see cref="EntryName"/> (<c>PICTURE_5.png</c>), байты как есть, в порядке
    /// <paramref name="pictures"/>. Файл уже есть — перезаписывается. Записи не сжимаются: PNG и JPEG уже сжаты, Deflate
    /// только тратит время.
    /// </summary>
    /// <exception cref="IOException">Файл не удалось записать.</exception>
    public static void Export(IReadOnlyList<PanelPicture> pictures, string path)
    {
        using var file = File.Create(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        foreach (var picture in pictures)
        {
            var entry = archive.CreateEntry(EntryName(picture), CompressionLevel.NoCompression);
            using var stream = entry.Open();
            stream.Write(picture.Data);
        }
    }

    /// <summary>
    /// Имя файла картинки <c>&lt;таблица&gt;_&lt;n&gt;.&lt;png|jpg&gt;</c>, n — номер элемента таблицы с единицы
    /// (<c>PICTURE_5.png</c>, <c>TILED_PICTURE_1.png</c>): запись архива и имя, которое показывает окно.
    /// </summary>
    /// <returns>Имя файла без каталога.</returns>
    public static string EntryName(PanelPicture picture) => $"{picture.Table}_{picture.Index + 1}.{picture.Extension}";
}
