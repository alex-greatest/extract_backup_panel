using System.Security.Cryptography;
using System.Text;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Сверка каталога hex-файлов со списком SHA-256 в формате <c>sha256sum</c>:
/// строки <c>&lt;хэш в нижнем регистре&gt;  &lt;путь через /&gt;</c>, отсортированные ординально по пути.
/// </summary>
public static class HexManifest
{
    /// <summary>Разделитель хэша и пути в строке списка, как у <c>sha256sum</c>.</summary>
    private const string Separator = "  ";

    /// <summary>
    /// Сравнивает файлы каталога со списком хэшей.
    /// </summary>
    /// <param name="directory">Каталог hex-файлов.</param>
    /// <param name="manifestPath">Файл списка хэшей.</param>
    /// <returns>Пустая строка, если всё совпало; иначе перечень изменённых, лишних и недостающих файлов.</returns>
    /// <exception cref="FormatException">Строка списка не в формате <c>sha256sum</c>.</exception>
    public static string Compare(string directory, string manifestPath)
    {
        var expected = Read(manifestPath);
        var actual = Compute(directory);
        var report = new StringBuilder();
        AppendGroup(report, "изменены", expected.Keys.Where(p => actual.TryGetValue(p, out var hash) && hash != expected[p]));
        AppendGroup(report, "лишние", actual.Keys.Where(p => !expected.ContainsKey(p)));
        AppendGroup(report, "недостающие", expected.Keys.Where(p => !actual.ContainsKey(p)));
        return report.ToString();
    }

    /// <summary>
    /// Считает SHA-256 всех файлов каталога (рекурсивно).
    /// </summary>
    /// <param name="directory">Каталог hex-файлов.</param>
    /// <returns>Хэш в нижнем регистре по относительному пути с <c>/</c>.</returns>
    private static SortedDictionary<string, string> Compute(string directory)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/');
            result[relative] = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        }
        return result;
    }

    /// <summary>
    /// Читает список хэшей; пустые строки пропускаются.
    /// </summary>
    /// <param name="manifestPath">Файл списка хэшей.</param>
    /// <returns>Хэш по относительному пути.</returns>
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

    /// <summary>
    /// Дописывает в отчёт группу путей с заголовком, если группа не пуста.
    /// </summary>
    /// <param name="report">Отчёт.</param>
    /// <param name="title">Заголовок группы.</param>
    /// <param name="paths">Пути файлов группы.</param>
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
