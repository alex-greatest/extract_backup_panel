using ClosedXML.Excel;
using FlexRt.Export;
using FlexRt.Export.PanelData;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Unit.Export.PanelData;

/// <summary>
/// Книга окна из готовых таблиц <see cref="ReportSheetsExporter.Export"/>: раскладка «Сводки», «Тегов» и обычной таблицы,
/// порядок листов, пустой список (<c>.claude/rules/выходы-и-запуск.md</c>, пункт об окне; <c>.claude/rules/окно.md</c>).
/// </summary>
public sealed class ReportSheetsExporterTests
{
    /// <summary>Листы пишутся в порядке списка, каждый — своей раскладкой.</summary>
    [Fact]
    public void Export_WritesSheetsInOrderWithTheirLayouts()
    {
        using var dir = new TestDirectory();
        var path = Path.Combine(dir.Root, "window.xlsx");
        ReportSheet[] sheets =
        [
            new("Сводка", new ReportTable(["Параметр", "Значение"], [["Файл панели", "a.fwc"], ["", ""], ["Тегов", 3L]]), [], true),
            new("Теги", new ReportTable(["Name"], [["Tag_1"]]), [("Всего тегов", 1)], false),
            new("Экраны", new ReportTable(["Name", "Number"], [["Root screen", 1L]]), [], false)
        ];

        ReportSheetsExporter.Export(sheets, path);

        using var workbook = new XLWorkbook(path);
        Assert.Equal(["Сводка", "Теги", "Экраны"], workbook.Worksheets.Select(s => s.Name));
        var summary = workbook.Worksheet("Сводка");
        Assert.Equal("Файл панели", summary.Cell("A1").GetString());
        Assert.Equal("a.fwc", summary.Cell("B1").GetString());
        Assert.True(summary.Cell("A2").IsEmpty());
        Assert.Equal("3", summary.Cell("B3").GetString());
        var tags = workbook.Worksheet("Теги");
        Assert.Equal("Всего тегов", tags.Cell("A1").GetString());
        Assert.Equal(1, tags.Cell("B1").GetValue<int>());
        Assert.Equal("Name", tags.Cell("A10").GetString());
        Assert.Equal("Tag_1", tags.Cell("A11").GetString());
        var screens = workbook.Worksheet("Экраны");
        Assert.Equal("Name", screens.Cell("A1").GetString());
        Assert.Equal(1, screens.Cell("B2").GetValue<int>());
    }

    /// <summary>Без листов книга не пишется: Excel не сохраняет книгу без листов.</summary>
    [Fact]
    public void Export_NoSheets_Throws()
    {
        using var dir = new TestDirectory();

        Assert.Throws<ArgumentException>(() => ReportSheetsExporter.Export([], dir.PanelDataWorkbook));
        Assert.False(File.Exists(dir.PanelDataWorkbook));
    }
}
