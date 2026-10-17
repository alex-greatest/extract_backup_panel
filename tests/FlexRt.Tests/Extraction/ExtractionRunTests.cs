using FlexRt.Extraction;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Extraction;

/// <summary>
/// Тесты <see cref="ExtractionRun"/> внутри процесса на файлах из <c>samples/</c> с подставным
/// получателем событий (<see cref="RecordingReporter"/>): порядок событий, код возврата,
/// «не поддерживается», независимость экспортов. Выходы — во временный каталог
/// (<see cref="TestDirectory"/>).
/// </summary>
public sealed class ExtractionRunTests
{
    /// <summary>Экспорт строк языков.</summary>
    private const string Strings = "Языки -> XLSX";

    /// <summary>Экспорт данных панели.</summary>
    private const string Panel = "Теги -> XLSX";

    /// <summary>Экспорт hex-файлов.</summary>
    private const string Hex = "Explode -> HEX";

    /// <summary>Экспорт картинок.</summary>
    private const string Pictures = "Картинки -> файлы";

    /// <summary>
    /// <c>samples/pdata.fwc</c> без проекта ПЛК: панель, «не найден», сопоставление, четыре
    /// успешных экспорта; код 0; в результате панель, сводка «не найден» и четыре итога без ошибок.
    /// </summary>
    [Fact]
    public void SamplePanel_WithoutPlc_ReportsEventsInOrder()
    {
        using var dir = new TestDirectory();
        var reporter = new RecordingReporter();
        var plc = dir.MissingFile();

        var result = ExtractionRun.Execute(Paths(dir, "samples/pdata.fwc", plc), reporter);

        Assert.Equal(["PanelRead", "PlcNotFound", "Matched", Ok(Strings), Ok(Panel), Ok(Hex), Ok(Pictures)], reporter.Events);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(16, result.Document?.Tags.Count);
        Assert.Null(result.PlcProject);
        Assert.Equal(16, result.Matches.Count);
        Assert.Empty(result.PlcErrors);
        Assert.Equal($"не найден: {plc}", result.Summary?.PlcSource);
        Assert.Equal([Strings, Panel, Hex, Pictures], result.Exports.Select(e => e.Title));
        Assert.All(result.Exports, e => Assert.Null(e.Error));
    }

    /// <summary>
    /// <c>samples/plc/pdata-one-connection.fwc</c> с <c>samples/plc/PEData.plf</c>: проект ПЛК
    /// прочитан из PEData.plf, код 0, проект в результате.
    /// </summary>
    [Fact]
    public void PlcPanel_WithPlcProject_ReadsPlcBeforeMatching()
    {
        using var dir = new TestDirectory();
        var reporter = new RecordingReporter();

        var result = ExtractionRun.Execute(Paths(dir, "samples/plc/pdata-one-connection.fwc", RepositoryPaths.InRepo("samples/plc/PEData.plf")), reporter);

        Assert.Equal(["PanelRead", "PlcRead plf", "Matched", Ok(Strings), Ok(Panel), Ok(Hex), Ok(Pictures)], reporter.Events);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(51, result.PlcProject?.Tags.Count);
    }

    /// <summary>
    /// <c>samples/bad.fwc</c>: только событие «панель не прочитана», код 1, панели в результате
    /// нет, экспортов нет, каталог результата не создан.
    /// </summary>
    [Fact]
    public void BadPanel_StopsAfterPanelReadFailed()
    {
        using var dir = new TestDirectory();
        var reporter = new RecordingReporter();

        var result = ExtractionRun.Execute(Paths(dir, "samples/bad.fwc", dir.MissingFile()), reporter);

        Assert.Equal(["PanelReadFailed"], reporter.Events);
        Assert.Equal(1, result.ExitCode);
        Assert.Null(result.Document);
        Assert.Empty(result.Exports);
        Assert.False(Directory.Exists(dir.OutDir));
    }

    /// <summary>
    /// <c>samples/plc/pdata.fwc</c> (два соединения) с заданным <c>samples/plc/PEData.plf</c>:
    /// «не поддерживается» сразу после панели, код 1, проект ПЛК не читается, экспортов нет,
    /// каталог результата не создан.
    /// </summary>
    [Fact]
    public void TwoConnectionsWithPlcData_IsUnsupported()
    {
        using var dir = new TestDirectory();
        var reporter = new RecordingReporter();
        var plc = RepositoryPaths.InRepo("samples/plc/PEData.plf");

        var result = ExtractionRun.Execute(Paths(dir, "samples/plc/pdata.fwc", plc), reporter);

        Assert.Equal(["PanelRead", "Unsupported"], reporter.Events);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal($"не поддерживается: соединений панели с ПЛК - 2, а данные ПЛК заданы ({plc}) - "
            + "сопоставление сделано только для панели с одним соединением; файлы не созданы", reporter.UnsupportedMessage);
        Assert.Null(result.PlcProject);
        Assert.Empty(result.Exports);
        Assert.False(Directory.Exists(dir.OutDir));
    }

    /// <summary>
    /// Путь ПЛК — пустая папка (нет <c>SIMATIC.S7S</c>): «не прочитан», затем сопоставление и
    /// сообщение об ошибке ПЛК перед экспортами; экспорты успешны, код 1 из-за ошибки ПЛК.
    /// </summary>
    [Fact]
    public void UnreadablePlc_ReportsPlcErrorBeforeExports()
    {
        using var dir = new TestDirectory();
        var reporter = new RecordingReporter();
        var plc = Directory.CreateDirectory(Path.Combine(dir.Root, "пустая-карта")).FullName;

        var result = ExtractionRun.Execute(Paths(dir, "samples/pdata.fwc", plc), reporter);

        Assert.Equal(["PanelRead", "PlcReadFailed", "Matched", "PlcError", Ok(Strings), Ok(Panel), Ok(Hex), Ok(Pictures)], reporter.Events);
        Assert.Equal(1, result.ExitCode);
        Assert.Single(result.PlcErrors);
        Assert.Equal($"не прочитан: {plc}", result.Summary?.PlcSource);
    }

    /// <summary>
    /// <c>panel_data.xlsx</c> занят папкой: экспорт данных панели с ошибкой, остальные
    /// экспорты всё равно выполнены, код 1.
    /// </summary>
    [Fact]
    public void FailedExport_DoesNotStopOtherExports()
    {
        using var dir = new TestDirectory();
        var reporter = new RecordingReporter();
        Directory.CreateDirectory(dir.PanelDataWorkbook);
        File.WriteAllText(Path.Combine(dir.PanelDataWorkbook, "занято.txt"), "");

        var result = ExtractionRun.Execute(Paths(dir, "samples/pdata.fwc", dir.MissingFile()), reporter);

        Assert.Equal(["PanelRead", "PlcNotFound", "Matched", Ok(Strings), Failed(Panel), Ok(Hex), Ok(Pictures)], reporter.Events);
        Assert.Equal(1, result.ExitCode);
        Assert.NotNull(result.Exports[1].Error);
    }

    /// <summary>
    /// Сообщение об успехе экспорта строк языков бросает исключение (например, лог не
    /// пишется): экспорт считается неудачным и о нём сообщено как об ошибке, остальные экспорты
    /// выполнены, код 1.
    /// </summary>
    [Fact]
    public void ReporterFailureOnSuccess_CountsAsFailedExport()
    {
        using var dir = new TestDirectory();
        var reporter = new RecordingReporter(failOnSuccessOf: Strings);

        var result = ExtractionRun.Execute(Paths(dir, "samples/pdata.fwc", dir.MissingFile()), reporter);

        Assert.Equal(["PanelRead", "PlcNotFound", "Matched", Failed(Strings), Ok(Panel), Ok(Hex), Ok(Pictures)], reporter.Events);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal([Strings, Panel, Hex, Pictures], result.Exports.Select(e => e.Title));
        Assert.IsType<IOException>(result.Exports[0].Error);
    }

    /// <summary>Пути запуска: панель из репозитория, проект ПЛК и выходы — в каталоге теста.</summary>
    /// <returns>Пути для <see cref="ExtractionRun.Execute"/>.</returns>
    private static ExtractionPaths Paths(TestDirectory dir, string panel, string plc) =>
        new(RepositoryPaths.InRepo(panel), plc, dir.StringsWorkbook, dir.PanelDataWorkbook, dir.HexDir, dir.PicturesDir);

    /// <summary>Событие успешного экспорта.</summary>
    /// <returns>Текст события <see cref="RecordingReporter"/>.</returns>
    private static string Ok(string title) => $"ExportFinished {title}: ok";

    /// <summary>Событие экспорта с ошибкой.</summary>
    /// <returns>Текст события <see cref="RecordingReporter"/>.</returns>
    private static string Failed(string title) => $"ExportFinished {title}: ошибка";
}
