using FlexRt.Extraction;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Интеграционные тесты: запуск <see cref="PanelRunner.Run"/> на файлах из <c>samples/</c> и сверка текста запуска,
/// листа «Теги» и книги строк языков с эталонами
/// в <c>samples/expected/</c>. Каждый тест пишет результат в свой временный каталог
/// (<see cref="TestDirectory"/>); <c>data\out</c> не трогается.
/// </summary>
public sealed class SampleRunTests
{
    /// <summary>
    /// <c>samples/pdata.fwc</c> (TIA V17, внутренние теги) без проекта ПЛК: без предупреждений, сводка,
    /// лист «Теги» совпадает с эталоном, 625 строк языков, разбиение таблиц совпадает с эталоном.
    /// </summary>
    [Fact]
    public void SamplePanel_WithoutPlc_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), null, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Тегов: 16");
        RunAssertions.HasLine(run.Output, "Строк языков: 625");
        Assert.DoesNotContain("Файл ПЛК:", run.Output);
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/теги.txt");
        SummarySheetDump.Contains(dir.PanelDataWorkbook,
            "IP панели=неизвестно",
            "Соединения с ПЛК=нет",
            $"Данные ПЛК={PanelSources.NoPlc}");
        RunAssertions.StringsSheet(PanelRunner.SaveStrings(run, dir), 625);
        RunAssertions.TablesMatch("samples/pdata.fwc", "samples/expected/pdata.sha256");
    }

    /// <summary>
    /// <c>samples/bad.fwc</c> (обрезанная копия): понятное сообщение среди ошибок запуска
    /// без трассы стека.
    /// </summary>
    [Fact]
    public void BadPanel_FailsWithReadableMessage()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/bad.fwc"), null, dir.OutDir);

        RunAssertions.HasProblems(run);
        Assert.Contains("Не удалось прочитать", run.Errors);
        Assert.Contains("bad.fwc", run.Errors);
        RunAssertions.NoStackTrace(run);
    }

    /// <summary>
    /// <c>samples/plc/pdata-one-connection.fwc</c> (тот же проект TIA V21 до второго соединения,
    /// шаг <c>05_absolute</c>) с <c>samples/plc/PEData.plf</c> (2 ПЛК): без предупреждений, все 47 PLC-тегов
    /// найдены, лист «Теги» совпадает с эталоном (его 67 строк совпали со снятым ранее с
    /// <c>pdata.fwc</c> эталоном, который сверен с проектом через Openness), сводка — IP панели,
    /// соединение, оба ПЛК с IP из проекта.
    /// </summary>
    [Fact]
    public void PlcPanel_OneConnectionWithPlcProject_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var plc = RepositoryPaths.InRepo("samples/plc/PEData.plf");
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/plc/pdata-one-connection.fwc"), plc, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Тегов: 66");
        RunAssertions.HasLine(run.Output, "Строк языков: 641");
        RunAssertions.HasLine(run.Output, $"Файл ПЛК: {plc} (ПЛК: 2, тегов ПЛК: 51)");
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
        RunAssertions.StringsSheet(PanelRunner.SaveStrings(run, dir), 641);
    }

    /// <summary>
    /// <c>samples/plc-udt/</c> — тот же тестовый проект с одним ПЛК, пользовательским типом
    /// <c>User_data_type_1</c>, тегом входов <c>1</c> этого типа и DB <c>Data_block_1.ddd</c>; в
    /// панели TIA оставил только используемые элементы — HMI-теги <c>xxxx_1.Element_1</c> и
    /// <c>xxxx_2.Element_1</c>. Без предупреждений, все 49 PLC-тегов найдены: член тега входов —
    /// <c>1.Element_1</c>, член DB — <c>Data_block_1.ddd.Element_1</c>, как в TIA; лист «Теги»
    /// совпадает с эталоном.
    /// </summary>
    [Fact]
    public void UdtPanel_TagAndDbMembers_MatchExpected()
    {
        using var dir = new TestDirectory();
        var plc = RepositoryPaths.InRepo("samples/plc-udt/PEData.plf");
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/plc-udt/pdata.fwc"), plc, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Тегов: 68");
        RunAssertions.HasLine(run.Output, $"Файл ПЛК: {plc} (ПЛК: 1, тегов ПЛК: 49)");
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/plc-udt/теги.txt");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "ПЛК=PLC_1", "Блоков данных (DB)=1");
    }

    /// <summary>
    /// <c>samples/plc/pdata.fwc</c> (два соединения с ПЛК) с <c>samples/plc/PEData.plf</c>:
    /// сознательно не поддерживается — сводка панели в тексте запуска, затем сообщение «не
    /// поддерживается» среди ошибок запуска, панель показана без данных ПЛК.
    /// </summary>
    [Fact]
    public void PlcPanel_TwoConnectionsWithPlcProject_Unsupported()
    {
        using var dir = new TestDirectory();
        var plc = RepositoryPaths.InRepo("samples/plc/PEData.plf");
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/plc/pdata.fwc"), plc, dir.OutDir);

        RunAssertions.Unsupported(run, 2, plc, dir);
        RunAssertions.HasLine(run.Output, "Тегов: 69");
    }
}
