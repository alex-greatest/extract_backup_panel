using FlexRt.Export;
using FlexRt.Export.PanelData;
using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Extraction;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Языки колонок таблиц листов (<see cref="ReportTable.ColumnLanguages"/>), по которым окно показывает колонки выбранного
/// языка. <c>samples/a603/pdata.fwc</c> — три языка панели: 0x409, 0x419, 0x415.
/// </summary>
public sealed class LanguageColumnsTests
{
    /// <summary>Языки <c>samples/a603/pdata.fwc</c> в порядке STRINGSTORE.</summary>
    private static readonly int[] A603Languages = [0x409, 0x419, 0x415];

    /// <summary>
    /// Листы с текстами на языках: язык помечен ровно у колонок, в заголовке которых этот язык
    /// (<see cref="LanguageText.Format"/>), у остальных — <c>null</c>; у «Классов алармов» каждый язык — у четырёх колонок
    /// (Display name, Incoming, Outgoing, Acknowledged), у остальных листов — у одной. Аналоговых алармов в a603 нет —
    /// таблица строится напрямую.
    /// </summary>
    [Fact]
    public void A603_LanguageColumns_MatchHeaders()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/a603/pdata.fwc"));
        var sheets = Sheets(doc);

        Assert.Equal(A603Languages, LanguageText.Languages(doc));
        AssertLanguages(sheets[SystemEventsSheet.Name], 1);
        AssertLanguages(sheets[DiscreteAlarmsSheet.Name], 1);
        AssertLanguages(AnalogAlarmsTable.Build(doc), 1);
        AssertLanguages(sheets[AlarmClassesSheet.Name], 4);
    }

    /// <summary>У листов без текстов на языках («Сводка», «Теги», «Экраны») языков колонок нет.</summary>
    [Fact]
    public void A603_OtherSheets_NoLanguageColumns()
    {
        var sheets = Sheets(FwxReader.Read(RepositoryPaths.InRepo("samples/a603/pdata.fwc")));
        string[] languageSheets = [SystemEventsSheet.Name, DiscreteAlarmsSheet.Name, AlarmClassesSheet.Name];

        Assert.Contains(TagsSheet.Name, sheets.Keys);
        Assert.All(sheets.Where(s => !languageSheets.Contains(s.Key)), s => Assert.Empty(s.Value.ColumnLanguages));
    }

    /// <summary>
    /// <c>samples/a603-classes/pdata.fwc</c>: у алармов своего класса ячейка Alarm class несёт Display name на каждом языке
    /// панели (для окна), а её текст — на первом языке, как в Excel; у стандартных классов текстов на языках нет.
    /// </summary>
    [Fact]
    public void A603Classes_CustomAlarmClass_HasLanguageTexts()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/a603-classes/pdata.fwc"));
        var table = Sheets(doc)[DiscreteAlarmsSheet.Name];
        var classColumn = table.Columns.ToList().IndexOf("Alarm class");
        var cells = table.Rows.Select(r => r[classColumn]).ToList();

        var custom = cells.Where(c => c.LanguageTexts is not null).ToList();
        Assert.NotEmpty(custom);
        Assert.All(custom, c =>
        {
            Assert.Equal(A603Languages, c.LanguageTexts!.Keys);
            Assert.Equal(c.LanguageTexts[0x409], c.Text);
        });
        Assert.Contains(custom, c => c.LanguageTexts![0x419] != c.Text);
        Assert.All(cells.Where(c => c.Text is "Errors" or "Warnings" or "Acknowledgement" or "No Acknowledgement"),
            c => Assert.Null(c.LanguageTexts));
    }

    /// <summary>Таблицы листов книги по имени листа, как их строит окно (без данных ПЛК); лист не построен — тест падает.</summary>
    /// <returns>Таблица по имени листа.</returns>
    private static Dictionary<string, ReportTable> Sheets(FwxDocument doc)
    {
        var built = PanelDataBook.TryBuild(PanelSources.From(doc, "pdata.fwc", null));
        Assert.All(built, s => Assert.Null(s.Error));
        return built.ToDictionary(s => s.Sheet.Name, s => s.Sheet.Table);
    }

    /// <summary>
    /// Языки колонок таблицы: по одному на колонку; у колонки с языком в заголовке — этот язык, у остальных — <c>null</c>;
    /// каждый язык панели — у <paramref name="groups"/> колонок.
    /// </summary>
    private static void AssertLanguages(ReportTable table, int groups)
    {
        Assert.Equal(table.Columns.Count, table.ColumnLanguages.Count);
        for (var i = 0; i < table.Columns.Count; i++)
        {
            var title = table.Columns[i];
            int? expected = A603Languages.Where(l => title.EndsWith(LanguageText.Format(l))).Select(l => (int?)l).FirstOrDefault();
            Assert.Equal(expected, table.ColumnLanguages[i]);
        }
        Assert.All(A603Languages, l => Assert.Equal(groups, table.ColumnLanguages.Count(c => c == l)));
    }
}
