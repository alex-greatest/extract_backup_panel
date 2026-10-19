using ClosedXML.Excel;
using FlexRt.Export;
using FlexRt.Export.PanelData;
using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Summary;
using FlexRt.Parsing.Matching;
using FlexRt.Parsing.Panel;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Unit.Export.PanelData;

/// <summary>
/// Книга окна (<see cref="ReportSheetsExporter"/> из тех же таблиц, что показывает окно) совпадает с книгой консоли
/// (<see cref="PanelDataExporter"/>) ячейка в ячейку, а консоль пишет листы в порядке
/// <see cref="PanelDataExporter.SheetOrder"/> (<c>.claude/rules/окно.md</c>, сохранение в Excel). Без данных ПЛК и
/// ошибок ПЛК: листа «Ошибки» нет ни в одной книге.
/// </summary>
public sealed class WindowWorkbookTests
{
    /// <summary>Образцы: системные события, классы и экраны; дискретные и аналоговые алармы с группами.</summary>
    public static TheoryData<string> Panels => ["samples/pdata.fwc", "samples/project1-groups/pdata.fwc"];

    /// <summary>Каждая ячейка книги окна равна ячейке книги консоли: значение, тип и заливка; листы и их порядок те же.</summary>
    [Theory]
    [MemberData(nameof(Panels))]
    public void WindowBook_EqualsConsoleBook(string panel)
    {
        using var dir = new TestDirectory();
        var (doc, matches, summary) = Read(panel);
        var console = Path.Combine(dir.Root, "console.xlsx");
        var window = Path.Combine(dir.Root, "window.xlsx");

        PanelDataExporter.Export(doc, null, summary, matches, [], console);
        ReportSheetsExporter.Export(Sheets(doc, matches, summary), window);

        Assert.Equal(Cells(console), Cells(window));
    }

    /// <summary>Консоль пишет листы в порядке <see cref="PanelDataExporter.SheetOrder"/>: окно упорядочивает по нему же.</summary>
    [Theory]
    [MemberData(nameof(Panels))]
    public void ConsoleBook_FollowsSheetOrder(string panel)
    {
        using var dir = new TestDirectory();
        var (doc, matches, summary) = Read(panel);
        var console = Path.Combine(dir.Root, "console.xlsx");

        PanelDataExporter.Export(doc, null, summary, matches, ["ошибка"], console);

        using var workbook = new XLWorkbook(console);
        var names = workbook.Worksheets.Select(s => s.Name).ToList();
        Assert.Equal(PanelDataExporter.SheetOrder.Where(names.Contains), names);
    }

    /// <summary>Прочитать образец без данных ПЛК: панель, сопоставление без проекта ПЛК, сводка как в окне.</summary>
    /// <returns>Панель, сопоставление тегов и сводка запуска.</returns>
    private static (FwxDocument Doc, IReadOnlyList<PlcMatch?> Matches, RunSummary Summary) Read(string panel)
    {
        var path = RepositoryPaths.InRepo(panel);
        var doc = FwxReader.Read(path);
        return (doc, TagMatcher.Match(doc, null), new RunSummary(path, new PanelFiles(null, null), "не задан"));
    }

    /// <summary>Листы книги окна из таблиц ядра — как разделы окна, при тех же условиях, что у консоли.</summary>
    /// <returns>Листы в порядке <see cref="PanelDataExporter.SheetOrder"/>.</returns>
    private static List<ReportSheet> Sheets(FwxDocument doc, IReadOnlyList<PlcMatch?> matches, RunSummary summary)
    {
        List<ReportSheet> sheets =
        [
            new(SummarySheet.Name, SummaryRows.Table(doc, null, summary), [], true),
            new(TagsSheet.Name, TagsTable.Build(doc, matches), TagStatistics.Rows(doc, matches), false)
        ];
        AddTable(sheets, SystemEventsSheet.Name, doc.SystemEvents.Count > 0, () => SystemEventsTable.Build(doc));
        AddTable(sheets, DiscreteAlarmsSheet.Name, doc.DiscreteAlarms.Count > 0, () => DiscreteAlarmsTable.Build(doc, matches));
        AddTable(sheets, AnalogAlarmsSheet.Name, doc.AnalogAlarms.Count > 0, () => AnalogAlarmsTable.Build(doc));
        AddTable(sheets, AlarmClassesSheet.Name, AlarmClassesTable.Classes(doc).Count > 0, () => AlarmClassesTable.Build(doc));
        AddTable(sheets, AlarmGroupsSheet.Name, AlarmGroupsTable.Groups(doc).Count > 0, () => AlarmGroupsTable.Build(doc));
        AddTable(sheets, ScreensSheet.Name, ScreensTable.Screens(doc).Count > 0, () => ScreensTable.Build(doc));
        return sheets;
    }

    /// <summary>Добавить лист-таблицу, если он есть в книге.</summary>
    private static void AddTable(List<ReportSheet> sheets, string name, bool exists, Func<ReportTable> table)
    {
        if (exists)
        {
            sheets.Add(new ReportSheet(name, table(), [], false));
        }
    }

    /// <summary>Все непустые ячейки книги: лист, адрес, тип, значение и заливка.</summary>
    /// <returns>Строки по порядку листов и ячеек.</returns>
    private static List<string> Cells(string path)
    {
        using var workbook = new XLWorkbook(path);
        return
        [
            .. workbook.Worksheets.SelectMany(sheet => sheet.CellsUsed().Select(c =>
                $"{sheet.Name}!{c.Address} {c.DataType} {c.Value} {c.Style.Fill.BackgroundColor} {c.Style.Font.FontColor}"))
        ];
    }
}
