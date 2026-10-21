using System.Security.Cryptography;
using System.Text;
using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Сверка разбиения всех таблиц TOC на элементы с эталоном в формате <c>sha256sum</c> (<c>samples/expected/*.sha256</c>):
/// строки <c>&lt;хэш в нижнем регистре&gt;  &lt;путь через /&gt;</c>. Эталон снят с hex-выгрузки удалённой консоли:
/// путь <c>0x&lt;id&gt;_&lt;имя&gt;/header.hex.txt</c> (0x34 байта заголовка) и <c>&lt;n&gt;_&lt;всего&gt;.hex.txt</c> на
/// элемент (<see cref="FwxReader.Items"/>), таблица не той раскладки — <c>raw.hex.txt</c> с байтами таблицы до конца
/// файла; содержимое — байты двузначным hex в нижнем регистре через пробел, без перевода строки в конце. Файлы не
/// пишутся: хэши считаются в памяти.
/// </summary>
public static class TableManifest
{
    /// <summary>Разделитель хэша и пути в строке эталона, как у <c>sha256sum</c>.</summary>
    private const string Separator = "  ";

    /// <summary>Сравнить разбиение таблиц панели с эталоном.</summary>
    /// <param name="doc">Прочитанная панель.</param>
    /// <param name="manifestPath">Файл эталона.</param>
    /// <returns>Пустая строка, если всё совпало; иначе перечень изменённых, лишних и недостающих путей.</returns>
    /// <exception cref="FormatException">Строка эталона не в формате <c>sha256sum</c>.</exception>
    public static string Compare(FwxDocument doc, string manifestPath)
    {
        var expected = Read(manifestPath);
        var actual = Compute(doc);
        var report = new StringBuilder();
        AppendGroup(report, "изменены", expected.Keys.Where(p => actual.TryGetValue(p, out var hash) && hash != expected[p]));
        AppendGroup(report, "лишние", actual.Keys.Where(p => !expected.ContainsKey(p)));
        AppendGroup(report, "недостающие", expected.Keys.Where(p => !actual.ContainsKey(p)));
        return report.ToString();
    }

    /// <summary>Хэши частей всех таблиц, как их раскладывала hex-выгрузка.</summary>
    /// <returns>Хэш по пути части.</returns>
    private static SortedDictionary<string, string> Compute(FwxDocument doc)
    {
        var b = doc.Binary;
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var table in doc.Toc)
        {
            var dir = $"0x{table.Id:x}_{SafeName(table.Name)}";
            try
            {
                b.Section = table.Name;
                var parts = new List<(string Name, string Hash)> { ("header.hex.txt", Hash(b, table.Offset, FwxReader.TableHeaderSize)) };
                parts.AddRange(FwxReader.Items(b, table).Select(i => ($"{i.Index + 1}_{table.Entries}.hex.txt", Hash(b, i.Offset, i.Length))));
                foreach (var (name, hash) in parts)
                {
                    result[$"{dir}/{name}"] = hash;
                }
            }
            catch (FwxFormatException)
            {
                var length = Math.Min(table.Offset + table.Size, b.Length) - table.Offset;
                if (length > 0)
                {
                    result[$"{dir}/raw.hex.txt"] = Hash(b, table.Offset, length);
                }
            }
        }
        return result;
    }

    /// <summary>SHA-256 текста <c>ef be 0c 00</c> байтов диапазона.</summary>
    /// <returns>Хэш в нижнем регистре.</returns>
    /// <exception cref="FwxFormatException">Диапазон выходит за пределы файла.</exception>
    private static string Hash(FwxBinary b, long pos, long length)
    {
        var bytes = b.Span(pos, length).ToArray();
        var text = string.Join(' ', bytes.Select(x => x.ToString("x2")));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(text)));
    }

    /// <summary>Имя таблицы в пути: недопустимые в имени папки символы — <c>_</c>, без точек и пробелов в конце.</summary>
    /// <returns>Имя папки таблицы.</returns>
    private static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string([.. name.Select(c => invalid.Contains(c) ? '_' : c)]).TrimEnd('.', ' ');
    }

    /// <summary>Прочитать эталон; пустые строки пропускаются.</summary>
    /// <returns>Хэш по пути.</returns>
    /// <exception cref="FormatException">В строке нет разделителя из двух пробелов.</exception>
    private static SortedDictionary<string, string> Read(string manifestPath)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(manifestPath).Where(l => l.Length > 0))
        {
            var split = line.IndexOf(Separator, StringComparison.Ordinal);
            if (split < 0)
            {
                throw new FormatException($"{manifestPath}: строка не в формате sha256sum: {line}");
            }
            result[line[(split + Separator.Length)..]] = line[..split];
        }
        return result;
    }

    /// <summary>Дописать в отчёт группу путей с заголовком, если группа не пуста.</summary>
    private static void AppendGroup(StringBuilder report, string title, IEnumerable<string> paths)
    {
        var list = paths.ToList();
        if (list.Count == 0)
        {
            return;
        }
        report.AppendLine($"{title} ({list.Count}):");
        foreach (var path in list)
        {
            report.AppendLine($"  {path}");
        }
    }
}
