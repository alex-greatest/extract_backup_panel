using FlexRt.Model.Panel;

namespace FlexRt.Export.Pictures;

/// <summary>Выгрузка картинок панели файлами, без привязки к экранам и объектам.</summary>
public static class PictureExporter
{
    /// <summary>
    /// Записать каждую картинку <see cref="FwxDocument.Pictures"/> в <paramref name="dir"/> файлом
    /// <c>&lt;таблица&gt;_&lt;n&gt;.&lt;png|jpg&gt;</c>, где n — номер элемента таблицы с единицы, как в
    /// hex-выводе (<c>PICTURE_5.png</c>, <c>TILED_PICTURE_1.png</c>). Одинаковые картинки пишутся
    /// каждая своим файлом. Перед записью каталог удаляется целиком, чтобы в нём не оставались
    /// файлы прошлого запуска; картинок нет — каталог остаётся пустым.
    /// </summary>
    /// <returns>Число записанных файлов.</returns>
    /// <exception cref="IOException">Каталог не удалось удалить или файл не удалось записать.</exception>
    public static int Export(FwxDocument doc, string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        Directory.CreateDirectory(dir);

        foreach (var picture in doc.Pictures)
        {
            File.WriteAllBytes(Path.Combine(dir, $"{picture.Table}_{picture.Index + 1}.{picture.Extension}"), picture.Data);
        }
        return doc.Pictures.Count;
    }
}
