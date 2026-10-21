using System.Text.RegularExpressions;
using ClosedXML.Excel;
using FlexRt.Extraction;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Проверки результата запуска <see cref="PanelRunner.Run"/>, общие для интеграционных тестов: есть ли предупреждения и
/// ошибки, строки текста запуска целиком, лист «Теги» и книга строк языков.
/// Сообщение о провале всегда показывает, что ожидалось и что получено.
/// </summary>
public static partial class RunAssertions
{
    /// <summary>
    /// Данные ПЛК сознательно не поддерживаются (у панели несколько соединений, данные ПЛК заданы): есть ошибка
    /// «ОШИБКА: не поддерживается…», без трассы стека; панель показана без данных ПЛК — на «Сводке» «Данные ПЛК=не задан».
    /// </summary>
    /// <param name="run">Результат запуска.</param>
    /// <param name="connections">Число соединений панели.</param>
    /// <param name="plc">Путь данных ПЛК, как передан запуску.</param>
    /// <param name="dir">Временный каталог теста с каталогом результата.</param>
    public static void Unsupported(PanelRun run, int connections, string plc, TestDirectory dir)
    {
        HasProblems(run);
        HasLine(run.Errors, $"ОШИБКА: не поддерживается: соединений панели с ПЛК - {connections}, а данные ПЛК заданы ({plc}) - "
            + $"сопоставление сделано только для панели с одним соединением; {PlcDataReader.UnsupportedConsequence}");
        NoStackTrace(run);
        SummarySheetDump.Contains(dir.PanelDataWorkbook, $"Данные ПЛК={PanelSources.NoPlc}");
    }

    /// <summary>Строка трассы стека .NET в английской или русской локали.</summary>
    /// <returns>Регулярное выражение, созданное при компиляции.</returns>
    [GeneratedRegex(@"^\s+(at|в) \S", RegexOptions.Multiline)]
    private static partial Regex StackTraceLine();

    /// <summary>Проверяет, что в тексте запуска нет предупреждений и ошибок; иначе показывает весь текст.</summary>
    /// <param name="run">Результат запуска.</param>
    public static void Clean(PanelRun run)
    {
        Assert.False(run.HasProblems, $"ожидался запуск без предупреждений и ошибок\n{Describe(run)}");
    }

    /// <summary>Проверяет, что в тексте запуска есть предупреждения или ошибки; иначе показывает весь текст.</summary>
    /// <param name="run">Результат запуска.</param>
    public static void HasProblems(PanelRun run)
    {
        Assert.True(run.HasProblems, $"ожидались предупреждения или ошибки\n{Describe(run)}");
    }

    /// <summary>
    /// Проверяет, что в выводе есть строка, равная <paramref name="line"/> целиком
    /// (<c>Тегов: 16</c> не совпадёт с <c>Тегов: 160</c>).
    /// </summary>
    /// <param name="output">Строки Info или предупреждения и ошибки запуска (<see cref="PanelRun"/>).</param>
    /// <param name="line">Ожидаемая строка без перевода строки.</param>
    public static void HasLine(string output, string line)
    {
        Assert.True(Lines(output).Contains(line), $"в выводе нет строки «{line}»:\n{output}");
    }

    /// <summary>Проверяет, что в тексте запуска нет трассы стека .</summary>
    /// <param name="run">Результат запуска.</param>
    public static void NoStackTrace(PanelRun run)
    {
        Assert.False(StackTraceLine().IsMatch(run.Errors), $"в предупреждениях и ошибках трасса стека:\n{run.Errors}");
        Assert.False(StackTraceLine().IsMatch(run.Output), $"в тексте запуска трасса стека:\n{run.Output}");
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

    /// <summary>Проверяет, что разбиение всех таблиц панели на элементы совпадает с эталоном (<see cref="TableManifest"/>).</summary>
    /// <param name="panel">Образец <c>pdata.fwc</c> относительно корня репозитория.</param>
    /// <param name="manifestPath">Эталон относительно корня репозитория.</param>
    public static void TablesMatch(string panel, string manifestPath)
    {
        var differences = TableManifest.Compare(FwxReader.Read(RepositoryPaths.InRepo(panel)), RepositoryPaths.InRepo(manifestPath));
        Assert.True(differences.Length == 0, $"разбиение таблиц отличается от {manifestPath}:\n{differences}");
    }

    /// <summary>Текст запуска для сообщения о провале: строки Info, предупреждения и ошибки.</summary>
    /// <param name="run">Результат запуска.</param>
    /// <returns>Многострочное описание запуска.</returns>
    private static string Describe(PanelRun run) =>
        $"текст запуска:\n{run.Output}\nпредупреждения и ошибки:\n{run.Errors}";

    /// <summary>Разбивает вывод на строки без <c>\r</c>.</summary>
    /// <param name="output">Текст запуска.</param>
    /// <returns>Строки вывода.</returns>
    private static string[] Lines(string output) => [.. output.Split('\n').Select(l => l.TrimEnd('\r'))];
}
