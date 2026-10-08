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
        SummarySheetDump.Contains(dir.PanelDataWorkbook,
            "Модель панели=неизвестно",
            "Версия Runtime=неизвестно",
            "IP панели=неизвестно",
            "Соединения с ПЛК=нет",
            $"Данные ПЛК=не найден: {plc}");
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
    /// <c>samples/plc/pdata-one-connection.fwc</c> (тот же проект TIA V21 до второго соединения,
    /// шаг <c>05_absolute</c>) с <c>samples/plc/PEData.plf</c> (2 ПЛК): код 0, все 47 PLC-тегов
    /// найдены, лист «Теги» совпадает с эталоном (его 67 строк совпали со снятым ранее с
    /// <c>pdata.fwc</c> эталоном, который сверен с проектом через Openness), сводка — IP панели,
    /// соединение, оба ПЛК с IP из проекта.
    /// </summary>
    [Fact]
    public void PlcPanel_OneConnectionWithPlcProject_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var plc = RepositoryPaths.InRepo("samples/plc/PEData.plf");
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/plc/pdata-one-connection.fwc"), plc, dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Тегов: 66");
        RunAssertions.HasLine(run.StdOut, "Строк языков: 641");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {plc} (ПЛК: 2, тегов ПЛК: 51)");
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/plc/теги.txt");
        SummarySheetDump.Contains(dir.PanelDataWorkbook,
            "IP панели=192.168.0.2 / 255.255.255.0",
            "Соединение HMI_Connection_1=IP ПЛК 192.168.0.1",
            $"Данные ПЛК=PEData.plf: {plc}",
            "ПЛК=PLC_1",
            "IP ПЛК (из данных ПЛК)=192.168.0.1, 192.168.1.1",
            "ПЛК=PLC_2",
            "IP ПЛК (из данных ПЛК)=192.168.1.1, 192.168.0.3",
            "Модель CPU=неизвестно",
            "Тегов ПЛК=51",
            "Блоков данных (DB)=0");
        RunAssertions.StringsSheet(dir.StringsWorkbook, 641);
    }

    /// <summary>
    /// <c>samples/plc/pdata.fwc</c> (два соединения с ПЛК) с <c>samples/plc/PEData.plf</c>:
    /// сознательно не поддерживается — сводка панели напечатана, затем сообщение «не
    /// поддерживается» в stderr, код 1, файлы результата не созданы.
    /// </summary>
    [Fact]
    public void PlcPanel_TwoConnectionsWithPlcProject_Unsupported()
    {
        using var dir = new TestDirectory();
        var plc = RepositoryPaths.InRepo("samples/plc/PEData.plf");
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/plc/pdata.fwc"), plc, dir.OutDir);

        RunAssertions.Unsupported(run, 2, plc, dir);
        RunAssertions.HasLine(run.StdOut, "Тегов: 69");
    }
}
