using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Системные события (таблица SYSMSGHANDLER): строка консоли, строка листа «Сводка» и лист
/// «Системные события» в <c>panel_data.xlsx</c>. Ожидаемые тексты сверены со списком
/// System events из TIA Portal (Project1, TIA V21).
/// </summary>
public sealed class SystemEventsRunTests
{
    /// <summary>
    /// <c>samples/pdata.fwc</c> (Project1, TIA V17, English): 585 событий по возрастанию ID,
    /// первое — 9999, последнее — 620000; текст с апострофом в начале сохраняется целиком.
    /// </summary>
    [Fact]
    public void SamplePanel_EnglishEvents()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Системных событий: 585");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Системных событий=585");
        var rows = SystemEventsSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(586, rows.Count);
        Assert.Equal("ID | 0x409 — English (United States)", rows[0]);
        Assert.Equal("9999 | Global: Unknown error %1,%2,%3,%4,%5,%6,%7,%8,%9.", rows[1]);
        Assert.Contains("10108 | Tag", rows);
        Assert.Contains("200102 | 'Project ID' area pointer: Error in type conversion.", rows);
        Assert.Equal("620000 | %1", rows[^1]);
    }

    /// <summary>
    /// <c>samples/card/pdata.fwc</c> (бэкап CCB-A13, TP1500 Comfort, Runtime V17, один язык —
    /// русский): колонка текста по языку панели, 585 событий.
    /// </summary>
    [Fact]
    public void CardPanel_RussianEvents()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/card/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Системных событий: 585");
        var rows = SystemEventsSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(586, rows.Count);
        Assert.Equal("ID | 0x419 — русский (Россия)", rows[0]);
        Assert.Contains("10108 | Переменная", rows);
        Assert.Contains("200102 | Указатель участка 'Опознавание проекта': ошибка при конвертировании типа.", rows);
    }
}
