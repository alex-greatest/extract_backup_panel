using FlexRt.Export.PanelData.Screens;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Цвет фона и подсказка на листе «Экраны» по шаговым копиям, сверенным с TIA (вне git): Project1
/// <c>data/real/steps-screen-props/</c> и A603A0097 <c>data/real/steps-tooltip-langs/</c>. Копии нет — тест падает.
/// </summary>
public sealed class ScreenPropsRunTests
{
    /// <summary>Заголовок листа «Экраны» панели с одним языком 0x409.</summary>
    private const string Header = ScreensRunTests.Header;

    /// <summary>Шаг 02: цвет фона <c>Screen_1</c> в TIA — 18, 52, 86; у остальных экранов — 182, 182, 182, как до шага.</summary>
    [Fact]
    public void BackgroundColorStep_ScreenOneColor()
    {
        var rows = ScreensSheet(StepCopies.ScreenProps, "02_bg_color.fwc");

        Assert.Equal(Header, rows[0]);
        Assert.Equal("Screen_1 | 2 |  | 800 | 420 | 18, 52, 86 |  | Screen", rows[3]);
        Assert.All(rows.Skip(1).Where(r => !r.StartsWith("Screen_1 ")), r => Assert.Contains(" | 182, 182, 182 | ", r));
    }

    /// <summary>Шаг 02: ячейка Background color закрашена своим цветом, как цвета на листе «Классы алармов».</summary>
    [Fact]
    public void BackgroundColorStep_CellFilledWithColor()
    {
        var table = ScreensTable.Build(FwxReader.Read(StepCopies.Path(StepCopies.ScreenProps, "02_bg_color.fwc")));
        var color = table.Columns.ToList().IndexOf("Background color");

        Assert.Equal(0x123456, table.Rows.Single(r => r[0].Text == "Screen_1")[color].FillRgb);
        Assert.Equal(0xB6B6B6, table.Rows.Single(r => r[0].Text == "Root screen")[color].FillRgb);
    }

    /// <summary>
    /// Шаг 13: подсказка <c>Screen_2</c>, введённая в интерфейсе TIA, — текст как есть (перенос CRLF; из книги ClosedXML
    /// читает его как <c>\n</c>); подсказка
    /// <c>Screen_1</c>, заданная на шаге 04 через Openness, — с разметкой, как в строке языка; у остальных — пусто.
    /// </summary>
    [Fact]
    public void TooltipStep_TextAsIs()
    {
        var rows = ScreensSheet(StepCopies.ScreenProps, "13_tooltip_ui_plain.fwc");

        Assert.Equal(Header, rows[0]);
        Assert.Equal("Screen_1 | 2 |  | 800 | 420 | 18, 52, 86 | <body><p>TipScreenOne</p></body> | Screen", rows[3]);
        Assert.Equal(@"Screen_2 | 5 | Yes | 800 | 420 | 182, 182, 182 | Line1 A&B <x>\nLine2 | Screen", rows[4]);
        Assert.All(rows.Skip(1).Where(r => !r.StartsWith("Screen_")), r => Assert.Contains(" | 182, 182, 182 |  | ", r));
    }

    /// <summary>
    /// Шаги 14 и 15: цвет фона Permanent area в TIA — 52, 86, 120, высота 60, на шаге 15 — 80 (из VIEW_DYN; запись
    /// <c>Overview</c> в SCREEN — 800×480 на обоих шагах). Высота экранов в TIA после шага 15 осталась 420, как в файле: смена Permanent area экраны не пересчитывает.
    /// </summary>
    [Theory]
    [InlineData("14_overview_color.fwc", 60)]
    [InlineData("15_overview_height.fwc", 80)]
    public void PermanentAreaSteps_HeightFromViewDyn(string step, int height)
    {
        var rows = ScreensSheet(StepCopies.ScreenProps, step);

        Assert.Equal(Header, rows[0]);
        Assert.Equal($"Permanent area |  |  | 800 | {height} | 52, 86, 120 |  | Permanent area", rows[1]);
        Assert.All(rows.Where(r => r.EndsWith(" | Screen")), r => Assert.Contains(" | 800 | 420 | ", r));
    }

    /// <summary>
    /// A603A0097, три языка Runtime: подсказка <c>998_LOGIN</c> — колонка на каждый язык в порядке STRINGSTORE; других
    /// подсказок нет.
    /// </summary>
    [Fact]
    public void LanguagesStep_TooltipPerLanguage()
    {
        var rows = ScreensSheet(StepCopies.TooltipLangs, "01_tooltip_langs.fwc");

        Assert.Equal(
            "Name | Number | Start screen | Width | Height | Background color | Tooltip 0x409 — English (United States) | "
            + "Tooltip 0x419 — русский (Россия) | Tooltip 0x415 — polski (Polska) | Вид", rows[0]);
        Assert.Equal(
            "998_LOGIN | 998 | Yes | 1366 | 768 | 182, 182, 182 | <body><p>TipLoginEN</p></body> | "
            + "<body><p>TipLoginRU Подсказка</p></body> | <body><p>TipLoginPL Podpowiedź</p></body> | Screen",
            Assert.Single(rows, r => r.Contains("Tip")));
    }

    /// <summary>Открыть шаговую копию шагами окна и прочитать лист «Экраны»; копия читается без предупреждений.</summary>
    /// <returns>Слепок листа, первая строка — заголовок.</returns>
    private static List<string> ScreensSheet(string folder, string step)
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(StepCopies.Path(folder, step), null, dir.OutDir);
        RunAssertions.Clean(run);
        return SheetDump.Read(dir.PanelDataWorkbook, "Экраны");
    }
}
