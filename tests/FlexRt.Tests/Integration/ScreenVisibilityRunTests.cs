using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Visibility экрана на листе «Экраны» по шаговым копиям Project1 <c>data/real/steps-animations/</c> (вне git), сверенным
/// с TIA: тег, From, To и Visible/Invisible анимации <c>Screen_2</c>. Копии нет — тест падает.
/// </summary>
public sealed class ScreenVisibilityRunTests
{
    /// <summary>
    /// Каждый шаг меняет одну настройку Visibility <c>Screen_2</c> в TIA: тег (Bool, массив Bool, Int, DInt, тег ПЛК
    /// DWord), From, To (Range) и Visible/Invisible; ячейки — как в TIA.
    /// </summary>
    [Theory]
    [InlineData("01_screen_visibility.fwc", "Bool_I0_0 | 0 | 0 | Visible")]
    [InlineData("02_screen_visibility_to1.fwc", "Bool_I0_0 | 0 | 1 | Visible")]
    [InlineData("03_screen_invisible.fwc", "Bool_I0_0 | 0 | 1 | Invisible")]
    [InlineData("04_tag_array_bool.fwc", "Array [0..22] of Bool_1 | 0 | 1 | Invisible")]
    [InlineData("04_tag_int_to130.fwc", "Tag_Int | 0 | 130 | Invisible")]
    [InlineData("05_from_minus5.fwc", "Tag_Int | -5 | 130 | Invisible")]
    [InlineData("06_from7_to7.fwc", "Tag_Int | 7 | 7 | Invisible")]
    [InlineData("07_dint_to100000.fwc", "DInt | 0 | 100000 | Invisible")]
    [InlineData("08_dword_plc_md26.fwc", "DWord_MD26 | 0 | 100000 | Invisible")]
    public void Steps_ScreenTwoVisibilityAsInTia(string step, string visibility)
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(StepCopies.Path(StepCopies.Animations, step), null, dir.OutDir);
        RunAssertions.Clean(run);
        var rows = SheetDump.Read(dir.PanelDataWorkbook, "Экраны");

        Assert.Equal(ScreensRunTests.Header, rows[0]);
        Assert.Contains($" | 182, 182, 182 | {visibility} |  | Screen", Assert.Single(rows, r => r.StartsWith("Screen_2 ")));
        Assert.All(rows.Skip(1).Where(r => !r.StartsWith("Screen_2 ")), r => Assert.Contains(" | 182, 182, 182 |  |  |  |  | ", r));
    }

    /// <summary>
    /// Шаг 08, тип значения From элемента LIMIT — 0x11 (не f64 и не i16): предупреждение с таблицей и смещением, From —
    /// <c>?</c>, тег, To и Visible/Invisible читаются.
    /// </summary>
    [Fact]
    public void UnknownValueType_FromUnknown()
    {
        using var dir = new TestDirectory();
        // +0x18 — u16 тип From: за u32 1, u32 R = 1, ссылкой на тег и 8 байтами флагов
        var (input, item) = CorruptedCopy.U16(dir, StepEight(), "LIMIT", 0, 0x18, 0x11);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - LIMIT: запись 1 @ 0x{item.Offset:X}: тип значения From 0x11 не известен, From не прочитано");
        RunAssertions.NoStackTrace(run);
        Assert.Contains(" | 182, 182, 182 | DWord_MD26 | ? | 100000 | Invisible |  | Screen", ScreenTwo(dir));
    }

    /// <summary>
    /// Шаг 08, число ссылок элемента LIMIT — 0x40: предупреждение с таблицей и смещением, у <c>Screen_2</c> во всех
    /// четырёх колонках Visibility — <c>?</c>, остальные строки листа «Экраны» — без Visibility.
    /// </summary>
    [Fact]
    public void BrokenLimitRecord_VisibilityUnknown()
    {
        using var dir = new TestDirectory();
        // +0x04 — u32 число ссылок, см. ReferenceHeader
        var (input, item) = CorruptedCopy.U32(dir, StepEight(), "LIMIT", 0, 0x04, 0x40);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - LIMIT @ 0x{item.Offset:X}: запись 1: 64 ссылок не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        Assert.Contains(" | 182, 182, 182 | ? | ? | ? | ? |  | Screen", ScreenTwo(dir));
    }

    /// <summary>Шаг 08 (тег ПЛК <c>DWord_MD26</c>, 0…100000, Invisible) — путь относительно корня репозитория; копии нет — тест падает.</summary>
    /// <returns>Путь к копии относительно корня репозитория.</returns>
    private static string StepEight()
    {
        const string step = "08_dword_plc_md26.fwc";
        StepCopies.Path(StepCopies.Animations, step);
        return $"{StepCopies.Animations}/{step}";
    }

    /// <summary>Строка <c>Screen_2</c> листа «Экраны» записанной книги.</summary>
    /// <returns>Слепок строки (ячейки через <c> | </c>).</returns>
    private static string ScreenTwo(TestDirectory dir) =>
        Assert.Single(SheetDump.Read(dir.PanelDataWorkbook, "Экраны"), r => r.StartsWith("Screen_2 "));
}
