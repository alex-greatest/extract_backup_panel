using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Тексты-подсказки элементов (INFO_HANDLER): образцы <c>samples/</c>, шаговые копии <c>data/real/steps-screen-props/</c>
/// и <c>data/real/steps-tooltip-langs/</c> (вне git; подсказки заданы в TIA V21 и сверены с ним). Испорченные входы —
/// копии <c>samples/pdata.fwc</c>, изменённые в рантайме во временном каталоге теста.
/// </summary>
public sealed class InfoTextsReadTests
{
    /// <summary>Id таблицы SCREEN.</summary>
    private const int ScreenTable = 0x7;

    /// <summary>Id таблицы VIEW_IN (поле ввода-вывода).</summary>
    private const int ViewInTable = 0x2;

    /// <summary>Id таблицы VIEW_BUTTONS (кнопка).</summary>
    private const int ViewButtonsTable = 0x17;

    /// <summary>Id таблицы MSG_NUMEVENT (дискретные алармы).</summary>
    private const int MsgNumEventTable = 0x1c;

    /// <summary>Шаговые копии свойств экранов Project1 относительно корня репозитория.</summary>
    private const string ScreenPropsDir = "data/real/steps-screen-props";

    /// <summary>Шаговые копии подсказки на трёх языках A603A0097 относительно корня репозитория.</summary>
    private const string LangsDir = "data/real/steps-tooltip-langs";

    /// <summary>Смещение u32 числа текстов: имя <c>InfoTextHandler</c> из 15 символов кончается на +0x24, за ним u32 (0).</summary>
    private const int CountOffset = 0x28;

    /// <summary>Подсказка поля ввода-вывода (шаг 11 и дальше).</summary>
    private static readonly InfoText IoField = new(new VarReference(ViewInTable, 0), 0x2AB);

    /// <summary>Подсказка <c>Screen_1</c> после шага 11 (номер строки сдвинулся с 0x2AA).</summary>
    private static readonly InfoText ScreenOne = new(new VarReference(ScreenTable, 3), 0x2A9);

    /// <summary>Подсказка кнопки шаблона (шаг 12 и дальше).</summary>
    private static readonly InfoText TemplateButton = new(new VarReference(ViewButtonsTable, 4), 0x2AC);

    /// <summary>Образцы без подсказок: INFO_HANDLER есть, текстов 0, без предупреждений.</summary>
    [Theory]
    [InlineData("samples/pdata.fwc")]
    [InlineData("samples/screens/05_popup.fwc")]
    [InlineData("samples/a603/pdata.fwc")]
    [InlineData("samples/a603-analog/pdata.fwc")]
    [InlineData("samples/a603-classes/pdata.fwc")]
    [InlineData("samples/plc/pdata.fwc")]
    [InlineData("samples/plc/pdata-one-connection.fwc")]
    [InlineData("samples/plc-udt/pdata.fwc")]
    [InlineData("samples/project1-classes/pdata.fwc")]
    [InlineData("samples/project1-groups/pdata.fwc")]
    public void Samples_EmptyTable(string sample)
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo(sample));

        Assert.Empty(doc.Warnings);
        Assert.NotNull(doc.FindTable("INFO_HANDLER"));
        Assert.Empty(doc.InfoTexts);
    }

    /// <summary>
    /// Бэкап CCB-A13 (Runtime V17): 2000 пар на элементы MSG_NUMEVENT 0…1999 по порядку (наблюдение: Info text
    /// дискретных алармов; с TIA не сверено, проекта нет), у каждой есть строка, все строки пустые.
    /// </summary>
    [Fact]
    public void Card_DiscreteAlarmPairs()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/card/pdata.fwc"));

        Assert.Empty(doc.Warnings);
        Assert.Equal(Enumerable.Range(0, 2000).Select(i => new VarReference(MsgNumEventTable, i)), doc.InfoTexts.Select(t => t.Owner));
        var indexes = doc.InfoTexts.Select(t => t.TextIndex).ToHashSet();
        var strings = doc.Strings.Where(s => indexes.Contains(s.Idx)).ToList();
        Assert.Equal(indexes.Count, strings.Select(s => s.Idx).Distinct().Count());
        Assert.All(strings, s => Assert.Equal("", s.Str));
    }

    /// <summary>Шаг 04: подсказка <c>Screen_1</c> (запись SCREEN 3).</summary>
    [Fact]
    public void ScreenTooltipStep_OnePair()
    {
        var doc = ReadStep(ScreenPropsDir, "04_tooltip.fwc");

        Assert.Empty(doc.Warnings);
        Assert.Equal([new InfoText(new VarReference(ScreenTable, 3), 0x2AA)], doc.InfoTexts);
        Assert.Equal("Screen_1", doc.Screens[3].Name);
    }

    /// <summary>Шаг 11: добавлена подсказка поля ввода-вывода (VIEW_IN 0); пары в порядке файла.</summary>
    [Fact]
    public void ObjectTooltipStep_Pairs()
    {
        var doc = ReadStep(ScreenPropsDir, "11_object_tooltip.fwc");

        Assert.Empty(doc.Warnings);
        Assert.Equal([IoField, ScreenOne], doc.InfoTexts);
    }

    /// <summary>Шаг 12: добавлена подсказка кнопки шаблона (VIEW_BUTTONS 4) — одна, запись кнопки общая.</summary>
    [Fact]
    public void TemplateButtonStep_Pairs()
    {
        var doc = ReadStep(ScreenPropsDir, "12_template_button_tooltip.fwc");

        Assert.Empty(doc.Warnings);
        Assert.Equal([IoField, ScreenOne, TemplateButton], doc.InfoTexts);
    }

    /// <summary>
    /// Шаг 13: подсказка <c>Screen_2</c> (запись SCREEN 1), введённая в интерфейсе TIA, — простой текст, символы как
    /// есть, перенос — CRLF.
    /// </summary>
    [Fact]
    public void PlainTextStep_PairsAndText()
    {
        var doc = ReadStep(ScreenPropsDir, "13_tooltip_ui_plain.fwc");

        Assert.Empty(doc.Warnings);
        Assert.Equal([IoField, new InfoText(new VarReference(ScreenTable, 1), 0x2B4), ScreenOne, TemplateButton], doc.InfoTexts);
        Assert.Equal("Screen_2", doc.Screens[1].Name);
        Assert.Equal([(0x409, "Line1 A&B <x>\r\nLine2")], Texts(doc, 0x2B4));
    }

    /// <summary>
    /// A603A0097, три языка Runtime: подсказка <c>998_LOGIN</c> — одна пара, на каждый язык строка с тем же номером.
    /// </summary>
    [Fact]
    public void LanguagesStep_OneTextPerLanguage()
    {
        var doc = ReadStep(LangsDir, "01_tooltip_langs.fwc");

        Assert.Empty(doc.Warnings);
        Assert.Equal([new InfoText(new VarReference(ScreenTable, 49), 0x2CDB)], doc.InfoTexts);
        Assert.Equal("998_LOGIN", doc.Screens[49].Name);
        Assert.Equal(
        [
            (0x409, "<body><p>TipLoginEN</p></body>"),
            (0x419, "<body><p>TipLoginRU Подсказка</p></body>"),
            (0x415, "<body><p>TipLoginPL Podpowiedź</p></body>")
        ], Texts(doc, 0x2CDB));
    }

    /// <summary>
    /// Первое слово элемента INFO_HANDLER — 7: предупреждение с таблицей и смещением, без stack trace; остальное
    /// читается.
    /// </summary>
    [Fact]
    public void BrokenMarker_Warns()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "INFO_HANDLER", 0, 0, 7);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - INFO_HANDLER @ 0x{item.Offset:X}: запись 1: первое слово 7, ожидалось 3");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Экранов: 1");
        Assert.Empty(run.Document!.InfoTexts);
    }

    /// <summary>Число текстов — 0xffff: пары не помещаются в элемент — предупреждение, текстов нет.</summary>
    [Fact]
    public void BrokenCount_Warns()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U32(dir, "samples/pdata.fwc", "INFO_HANDLER", 0, CountOffset, 0xffff);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - INFO_HANDLER @ 0x{item.Offset:X}: запись 1: 65535 текстов не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        Assert.Empty(run.Document!.InfoTexts);
    }

    /// <summary>
    /// Прочитать шаговую копию. Файла нет (копии не в git) — провал с понятным сообщением, а не молчаливый успех.
    /// </summary>
    /// <returns>Прочитанная панель.</returns>
    private static FwxDocument ReadStep(string folder, string step)
    {
        var path = RepositoryPaths.InRepo($"{folder}/{step}");
        Assert.True(File.Exists(path), $"нет {folder}/{step} (шаговые копии вне git): проверка подсказок невозможна");
        return FwxReader.Read(path);
    }

    /// <summary>Строки всех языков с номером <paramref name="index"/> в порядке STRINGSTORE: код языка и текст.</summary>
    /// <returns>Пары (код языка, текст).</returns>
    private static IEnumerable<(int, string)> Texts(FwxDocument doc, int index) =>
        doc.Strings.Where(s => s.Idx == index).Select(s => (s.Lcid, s.Str));
}
