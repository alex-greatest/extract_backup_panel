using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Проверки результата запуска FlexRt, общие для интеграционных тестов: код возврата,
/// строки консоли целиком, лист «Теги», лист «Ошибки», книга строк языков и hex-файлы.
/// Сообщение о провале всегда показывает, что ожидалось и что получено.
/// </summary>
public static partial class RunAssertions
{
    /// <summary>
    /// Запуск сознательно не поддерживается (у панели несколько соединений, данные ПЛК заданы):
    /// код 1, строка «ОШИБКА: не поддерживается…» в stderr, без трассы стека, файлы результата
    /// (<c>panel_data.xlsx</c>, <c>PDATA.xlsx</c>, папка <c>pdata</c>) не созданы.
    /// </summary>
    /// <param name="run">Результат запуска.</param>
    /// <param name="connections">Число соединений панели.</param>
    /// <param name="plc">Путь данных ПЛК, как передан программе.</param>
    /// <param name="dir">Временный каталог теста с каталогом результата.</param>
    public static void Unsupported(ProgramRun run, int connections, string plc, TestDirectory dir)
    {
        ExitCode(run, 1);
        HasLine(run.StdErr, $"ОШИБКА: не поддерживается: у панели {connections} соединения с ПЛК, а данные ПЛК заданы ({plc}) - "
            + "сопоставление сделано только для панели с одним соединением; файлы не созданы");
        NoStackTrace(run);
        Assert.False(File.Exists(dir.PanelDataWorkbook), "panel_data.xlsx создан");
        Assert.False(File.Exists(dir.StringsWorkbook), "PDATA.xlsx создан");
        Assert.False(Directory.Exists(dir.HexDir), "папка pdata создана");
    }

    /// <summary>Строка трассы стека .NET в английской или русской локали.</summary>
    /// <returns>Регулярное выражение, созданное при компиляции.</returns>
    [GeneratedRegex(@"^\s+(at|в) \S", RegexOptions.Multiline)]
    private static partial Regex StackTraceLine();

    /// <summary>Проверяет код возврата; при отличии показывает весь вывод запуска.</summary>
    /// <param name="run">Результат запуска.</param>
    /// <param name="expected">Ожидаемый код возврата.</param>
    public static void ExitCode(ProgramRun run, int expected)
    {
        Assert.True(run.ExitCode == expected, $"ожидался код возврата {expected}\n{Describe(run)}");
    }

    /// <summary>
    /// Проверяет, что в выводе есть строка, равная <paramref name="line"/> целиком
    /// (<c>Тегов: 16</c> не совпадёт с <c>Тегов: 160</c>).
    /// </summary>
    /// <param name="output">Весь stdout или stderr.</param>
    /// <param name="line">Ожидаемая строка без перевода строки.</param>
    public static void HasLine(string output, string line)
    {
        Assert.True(Lines(output).Contains(line), $"в выводе нет строки «{line}»:\n{output}");
    }

    /// <summary>Проверяет, что ни в stdout, ни в stderr нет трассы стека.</summary>
    /// <param name="run">Результат запуска.</param>
    public static void NoStackTrace(ProgramRun run)
    {
        Assert.False(StackTraceLine().IsMatch(run.StdErr), $"в stderr трасса стека:\n{run.StdErr}");
        Assert.False(StackTraceLine().IsMatch(run.StdOut), $"в stdout трасса стека:\n{run.StdOut}");
    }

    /// <summary>
    /// Проверяет, что слепок листа «Теги» построчно равен эталону; при отличии сообщение
    /// перечисляет каждую несовпавшую строку целиком.
    /// </summary>
    /// <param name="workbook">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="expectedPath">Эталон относительно корня репозитория.</param>
    public static void TagSheetEquals(string workbook, string expectedPath)
    {
        TagSheetEquals(workbook, File.ReadAllLines(RepositoryPaths.InRepo(expectedPath)), expectedPath);
    }

    /// <summary>
    /// Проверяет, что слепок листа «Теги» построчно равен ожидаемым строкам; при отличии
    /// сообщение перечисляет каждую несовпавшую строку целиком.
    /// </summary>
    /// <param name="workbook">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="expected">Ожидаемые строки слепка (формат <see cref="TagSheetDump"/>).</param>
    /// <param name="expectedPath">Откуда взято ожидание — для сообщения о провале.</param>
    public static void TagSheetEquals(string workbook, IReadOnlyList<string> expected, string expectedPath)
    {
        var actual = TagSheetDump.Read(workbook);
        var differences = Enumerable.Range(0, Math.Max(expected.Count, actual.Count))
            .Select(i => (Line: i + 1, Expected: i < expected.Count ? expected[i] : "<нет>", Actual: i < actual.Count ? actual[i] : "<нет>"))
            .Where(d => d.Expected != d.Actual)
            .Select(d => $"строка {d.Line}:\n  ожидалось: {d.Expected}\n  получено:  {d.Actual}")
            .ToList();
        Assert.True(differences.Count == 0, $"лист «Теги» отличается от {expectedPath}:\n{string.Join("\n", differences)}");
    }

    /// <summary>
    /// Проверяет книгу строк языков: лист <c>0x409</c> с заголовком <c>idx</c>/<c>str</c>
    /// и заданным числом строк данных после него.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>PDATA.xlsx</c>.</param>
    /// <param name="strings">Ожидаемое число строк языка.</param>
    public static void StringsSheet(string workbookPath, int strings)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.True(workbook.TryGetWorksheet("0x409", out var sheet), "в PDATA.xlsx нет листа 0x409");
        Assert.Equal("idx", sheet.Cell(1, 1).GetString());
        Assert.Equal("str", sheet.Cell(1, 2).GetString());
        Assert.Equal(strings + 1, sheet.LastRowUsed()?.RowNumber());
    }

    /// <summary>Проверяет, что hex-файлы каталога совпадают со списком SHA-256.</summary>
    /// <param name="hexDir">Каталог hex-файлов.</param>
    /// <param name="manifestPath">Список хэшей относительно корня репозитория.</param>
    public static void HexMatches(string hexDir, string manifestPath)
    {
        var differences = HexManifest.Compare(hexDir, RepositoryPaths.InRepo(manifestPath));
        Assert.True(differences.Length == 0, $"hex-вывод отличается от {manifestPath}:\n{differences}");
    }

    /// <summary>
    /// Проверяет лист «Ошибки»: заголовок «Сообщение» в A1 и среди сообщений есть
    /// содержащее <paramref name="fragment"/>.
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="fragment">Ожидаемая часть сообщения.</param>
    public static void ErrorsSheetContains(string workbookPath, string fragment)
    {
        using var workbook = new XLWorkbook(workbookPath);
        Assert.True(workbook.TryGetWorksheet("Ошибки", out var sheet), "в panel_data.xlsx нет листа «Ошибки»");
        Assert.Equal("Сообщение", sheet.Cell(1, 1).GetString());
        var messages = sheet.Column(1).CellsUsed().Skip(1).Select(c => c.GetString()).ToList();
        Assert.True(messages.Any(m => m.Contains(fragment, StringComparison.Ordinal)),
            $"на листе «Ошибки» нет сообщения с «{fragment}»; есть:\n{string.Join("\n", messages)}");
    }

    /// <summary>Текст запуска для сообщения о провале: код возврата, stdout и stderr.</summary>
    /// <param name="run">Результат запуска.</param>
    /// <returns>Многострочное описание запуска.</returns>
    private static string Describe(ProgramRun run) =>
        $"код возврата {run.ExitCode}\nstdout:\n{run.StdOut}\nstderr:\n{run.StdErr}";

    /// <summary>Разбивает вывод на строки без <c>\r</c>.</summary>
    /// <param name="output">Весь вывод потока.</param>
    /// <returns>Строки вывода.</returns>
    private static string[] Lines(string output) => [.. output.Split('\n').Select(l => l.TrimEnd('\r'))];
}
