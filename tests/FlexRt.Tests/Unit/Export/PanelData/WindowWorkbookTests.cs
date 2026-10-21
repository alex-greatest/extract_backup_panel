using ClosedXML.Excel;
using FlexRt.Export.PanelData;
using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Extraction;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Summary;
using FlexRt.Parsing.Matching;
using FlexRt.Parsing.Panel;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Unit.Export.PanelData;

/// <summary>
/// Книга окна — листы <see cref="PanelDataBook.TryBuild(PanelDataSource)"/>, записанные <see cref="ReportSheetsExporter"/>,
/// как их собирает окно, — листы описания книги (<see cref="PanelDataBook.Sheets"/>) по порядку; сбой одного листа у окна
/// не мешает остальным (<c>.claude/rules/окно.md</c>, сохранение в Excel). Без данных ПЛК.
/// </summary>
public sealed class WindowWorkbookTests
{
    /// <summary>Образцы: системные события, классы и экраны; дискретные и аналоговые алармы с группами.</summary>
    public static TheoryData<string> Panels => ["samples/pdata.fwc", "samples/project1-groups/pdata.fwc"];

    /// <summary>
    /// Книга окна — листы, которые есть по описанию книги (<see cref="PanelDataSheet.Exists"/>), в его порядке; все
    /// построены, листа «Ошибки» нет.
    /// </summary>
    [Theory]
    [MemberData(nameof(Panels))]
    public void WindowBook_FollowsBookDescription(string panel)
    {
        using var dir = new TestDirectory();
        var (doc, matches, summary) = Read(panel);
        var window = Path.Combine(dir.Root, "window.xlsx");

        var built = PanelDataBook.TryBuild(new PanelDataSource(doc, null, summary, new Lazy<IReadOnlyList<PlcMatch?>>(() => matches)));
        Assert.All(built, b => Assert.Null(b.Error));
        ReportSheetsExporter.Export([.. built.Select(b => b.Sheet)], window);

        using var workbook = new XLWorkbook(window);
        Assert.Equal(Present(doc), workbook.Worksheets.Select(s => s.Name));
    }

    /// <summary>
    /// Порядок листов книги закреплён здесь отдельно от <see cref="PanelDataBook.Sheets"/>: его меняет только решение
    /// пользователя.
    /// </summary>
    [Fact]
    public void Sheets_InBookOrder()
    {
        Assert.Equal(
            ["Сводка", "Теги", "Системные события", "Дискретные алармы", "Аналоговые алармы", "Классы алармов", "Группы алармов", "Экраны"],
            PanelDataBook.Sheets.Select(s => s.Name));
    }

    /// <summary>
    /// Сопоставление с ПЛК падает: не построены только листы, которым оно нужно («Теги», «Дискретные алармы»), остальные
    /// построены, порядок листов тот же; у не построенного листа — пустая таблица.
    /// </summary>
    [Fact]
    public void TryBuild_FailedMatching_FailsOnlyItsSheets()
    {
        var (doc, _, summary) = Read("samples/project1-groups/pdata.fwc");

        var failing = new Lazy<IReadOnlyList<PlcMatch?>>(() => throw new InvalidOperationException("сбой"));

        var built = PanelDataBook.TryBuild(new PanelDataSource(doc, null, summary, failing));

        Assert.Equal(Present(doc), built.Select(b => b.Sheet.Name));
        Assert.Equal([TagsSheet.Name, DiscreteAlarmsSheet.Name], built.Where(b => b.Error is not null).Select(b => b.Sheet.Name));
        Assert.All(built.Where(b => b.Error is not null), b => Assert.Empty(b.Sheet.Table.Rows));
    }

    /// <summary>Прочитать образец без данных ПЛК: панель, сопоставление без проекта ПЛК, сводка как в окне.</summary>
    /// <returns>Панель, сопоставление тегов и сводка запуска.</returns>
    private static (FwxDocument Doc, IReadOnlyList<PlcMatch?> Matches, RunSummary Summary) Read(string panel)
    {
        var path = RepositoryPaths.InRepo(panel);
        var doc = FwxReader.Read(path);
        return (doc, TagMatcher.Match(doc, null), new RunSummary(path, PanelSources.NoPlc));
    }

    /// <summary>Имена листов, которые есть в книге по описанию (<see cref="PanelDataSheet.Exists"/>), по порядку.</summary>
    /// <returns>Имена листов.</returns>
    private static IEnumerable<string> Present(FwxDocument doc) => PanelDataBook.Sheets.Where(s => s.Exists(doc)).Select(s => s.Name);
}
