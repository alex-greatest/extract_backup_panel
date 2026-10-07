using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Интеграционные тесты: запуск FlexRt на файлах из <c>samples/</c> и сверка консоли,
/// кода возврата, листа «Теги», книги строк языков и hex-файлов с эталонами
/// в <c>samples/expected/</c>. Каждый тест пишет результат в свой временный каталог
/// (<c>FLEXRT_OUT</c>, см. <see cref="TestDirectory"/>); <c>data\out</c> не трогается.
/// </summary>
public sealed class SampleRunTests
{
    /// <summary>
    /// <c>samples/pdata.fwc</c> (TIA V17, внутренние теги) без проекта ПЛК: код 0, сводка,
    /// лист «Теги», 625 строк языков и hex-файлы совпадают с эталонами.
    /// </summary>
    [Fact]
    public void SamplePanel_WithoutPlc_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var plc = dir.MissingFile();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), plc, dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Тегов: 16");
        RunAssertions.HasLine(run.StdOut, "Строк языков: 625");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {plc} не найден - данные ПЛК: неизвестно");
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/теги.txt");
        RunAssertions.StringsSheet(dir.StringsWorkbook, 625);
        RunAssertions.HexMatches(dir.HexDir, "samples/expected/pdata.sha256");
    }

    /// <summary>
    /// <c>samples/bad.fwc</c> (обрезанная копия): код 1, понятное сообщение в stderr
    /// без трассы стека.
    /// </summary>
    [Fact]
    public void BadPanel_FailsWithReadableMessage()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/bad.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        Assert.Contains("Не удалось прочитать", run.StdErr);
        Assert.Contains("bad.fwc", run.StdErr);
        RunAssertions.NoStackTrace(run);
    }

    /// <summary>
    /// <c>samples/plc/pdata.fwc</c> с <c>samples/plc/PEData.plf</c> (TIA V21, PLC-теги,
    /// два соединения): код 0, сводка панели и ПЛК, лист «Теги», 645 строк языков и
    /// hex-файлы совпадают с эталонами.
    /// </summary>
    [Fact]
    public void PlcPanel_WithPlcProject_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var plc = RepositoryPaths.InRepo("samples/plc/PEData.plf");
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/plc/pdata.fwc"), plc, dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Тегов: 69");
        RunAssertions.HasLine(run.StdOut, "Строк языков: 645");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {plc} (ПЛК: 2, тегов ПЛК: 51)");
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/plc/теги.txt");
        RunAssertions.StringsSheet(dir.StringsWorkbook, 645);
        RunAssertions.HexMatches(dir.HexDir, "samples/expected/plc/pdata.sha256");
    }
}
