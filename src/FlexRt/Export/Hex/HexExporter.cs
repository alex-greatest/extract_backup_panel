using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;

namespace FlexRt.Export.Hex;

/// <summary>Разложить FWX по папкам: таблица TOC = папка, элемент = hex-файл.</summary>
public static class HexExporter
{
    /// <summary>Имя таблицы приходит из файла: недопустимые в имени папки символы заменяются на <c>_</c>.</summary>
    private static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).TrimEnd('.', ' ');
    }

    /// <summary>
    /// Папка <c>0x&lt;id&gt;_&lt;имя&gt;</c> на таблицу: <c>header.hex.txt</c> (0x34 байта) и <c>&lt;n&gt;_&lt;всего&gt;.hex.txt</c> на элемент.
    /// Перед записью каталог <paramref name="dir"/> удаляется целиком со всем содержимым,
    /// чтобы в нём не оставались файлы прошлого запуска.
    /// Таблица сначала разбирается целиком и только потом пишется. Если она не в формате
    /// «каталог смещений», сохраняется целиком в <c>raw.hex.txt</c>, сообщение попадает
    /// в <paramref name="warnings"/>, остальные таблицы обрабатываются.
    /// </summary>
    /// <returns>Число записанных файлов.</returns>
    /// <exception cref="IOException">Каталог не удалось удалить или файл не удалось записать.</exception>
    public static int Export(FwxDocument doc, string dir, List<string> warnings)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }

        var b = doc.Binary;
        var files = 0;
        foreach (var entry in doc.Toc)
        {
            var subDir = Path.Combine(dir, $"0x{entry.Id:x}_{SafeName(entry.Name)}");
            Directory.CreateDirectory(subDir);

            try
            {
                b.Section = entry.Name;
                var header = b.ToHex(entry.Offset, FwxReader.TableHeaderSize);
                var items = FwxReader.Items(b, entry).Select(i => (i.Index, Hex: b.ToHex(i.Offset, i.Length))).ToList();

                File.WriteAllText(Path.Combine(subDir, "header.hex.txt"), header);
                foreach (var (index, hex) in items)
                {
                    File.WriteAllText(Path.Combine(subDir, $"{index + 1}_{entry.Entries}.hex.txt"), hex);
                }
                files += 1 + items.Count;
            }
            catch (FwxFormatException e)
            {
                warnings.Add(e.Message);
                files += WriteWholeTable(b, entry, subDir);
            }
        }
        return files;
    }

    /// <summary>
    /// Таблица не подошла под «каталог смещений»: сохранить её байты целиком
    /// в <c>raw.hex.txt</c>. Хвост за концом файла отбрасывается.
    /// </summary>
    /// <returns>Число записанных файлов: 1, либо 0, если от таблицы ничего не осталось.</returns>
    private static int WriteWholeTable(FwxBinary b, TocEntry entry, string subDir)
    {
        var length = Math.Min(entry.Offset + entry.Size, b.Length) - entry.Offset;
        if (length <= 0)
        {
            return 0;
        }
        File.WriteAllText(Path.Combine(subDir, "raw.hex.txt"), b.ToHex(entry.Offset, length));
        return 1;
    }
}
