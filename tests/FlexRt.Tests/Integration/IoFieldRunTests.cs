using System.Xml.Linq;
using FlexRt.Export.ScreenObjects;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// I/O field (<see cref="FwxDocument.IoFields"/>) против TIA: шаговые копии <c>data/real/steps-iofield/</c> (вне git) с
/// XML экрана из Openness рядом, A603A0097 и испорченные копии <c>samples/screens/05_popup.fwc</c>.
/// </summary>
public sealed class IoFieldRunTests
{
    /// <summary>Шаги: имя копии без расширения; рядом — XML экрана Screen_2 из TIA с тем же именем.</summary>
    public static readonly TheoryData<string> Steps =
    [
        "00_base", "01_tag_int", "02_tag_dint", "03_tag_plc", "04_mode_input", "05_mode_output", "06_mode_io", "07_hex",
        "08_binary", "09_tag_real", "10_pattern_2dec", "11_pattern_sign", "12_leading_zeros", "13_int_shift2", "14_string",
        "15_back_color", "16_fore_color", "17_border_color", "18_border_back_color", "19_border_width", "20_corner_radius",
        "21_edge_solid", "22_fill_transparent", "23_unit_mm", "24_halign_center", "25_halign_right", "26_valign_top",
        "27_valign_bottom", "28_font_size20", "29_font_regular", "30_font_bold_italic", "31_font_bold_underline",
        "32_font_arial", "34_output_sign", "35_output_leading_zeros", "36_output_unit_mm", "37_format_date",
        "38_format_datetime", "39_format_time", "40_field_length5", "41_edge_dash", "42_edge_dot", "43_edge_dashdot",
        "44_edge_dashdotdot", "45_font_courier"
    ];


    /// <summary>
    /// Шаг, на котором TIA пишет в FONT не тот шрифт, что в свойствах: Arial → <c>Tahoma</c> (Courier New на шаге 45
    /// записан как есть — TIA подменяет шрифт при компиляции).
    /// </summary>
    private const string FontMismatchStep = "32_font_arial";

    /// <summary>
    /// Каждый шаг: поле <c>I/O field_1</c> Screen_2 сходится с XML TIA — режим, тег, формат (Date 4, Time 5, Date/time 6),
    /// длина шаблона (у Date, Time и Date/time — Field length; у Decimal Field length в файл не попадает — шаг 40), знаки
    /// после запятой (у Real — из шаблона, у целого — ShiftDecimalPoint), собранный Format pattern
    /// (<see cref="IoFieldTexts.Pattern"/>; у Date, Time и Date/time — пусто, в TIA поле недоступно),
    /// знак, ведущие нули, Units, цвета, рамка (Dash, Dot, DashDot, DashDotDot — тот же код, что Solid), углы,
    /// выравнивание, размер и начертание шрифта (имя — кроме шага 32); предупреждений нет. Fill pattern в файл не
    /// попадает — не сверяется.
    /// </summary>
    [Theory]
    [MemberData(nameof(Steps))]
    public void Step_FieldAsInTia(string step)
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.IoField, $"{step}.fwc"));
        var tia = XDocument.Load(StepCopies.Path(StepCopies.IoField, $"{step}.xml")).Descendants("Hmi.Screen.IOField").Single();

        Assert.Empty(doc.Warnings);
        var field = Field(doc, "Screen_2", A("ObjectName"));
        var pattern = A("FormatPattern");
        Assert.Equal(A("Mode"), field.Mode switch { IoFieldMode.Input => "Input", IoFieldMode.Output => "Output", _ => "InOutput" });
        Assert.Equal(Tag(tia), field.Tag);
        Assert.Null(field.TagVia);
        Assert.Equal(
            A("DataFormat"),
            field.DisplayFormat switch { 0 => "String", 1 => "Decimal", 2 => "Hexadecimal", 3 => "Binary", 4 => "Date", 5 => "Time", _ => "DateTime" });
        Assert.Equal(field.DisplayFormat >= 4 ? int.Parse(A("FieldLength")) : pattern.Length, field.PatternLength);
        Assert.Equal(field.DisplayFormat >= 4 ? "" : pattern, IoFieldTexts.Pattern(field));
        Assert.Equal(pattern.Contains('.') ? pattern.Length - pattern.IndexOf('.') - 1 : int.Parse(A("ShiftDecimalPoint")), field.Decimals);
        Assert.Equal(pattern.StartsWith('s'), field.Signed);
        Assert.Equal(bool.Parse(A("ShowLeadingZeros")), field.LeadingZeros);
        Assert.Equal(A("Unit"), field.Unit);
        Assert.Equal(Rgb(A("BackColor")), field.Fill.ColorRgb);
        Assert.Equal(Rgb(A("ForeColor")), field.Text.ColorRgb);
        Assert.Equal(
            new BorderStyle(A("EdgeStyle") == "Double" ? 2 : 1, int.Parse(A("BorderWidth")), Rgb(A("BorderColor")), Rgb(A("BorderBackColor")), int.Parse(A("CornerRadius"))),
            field.Border);
        Assert.Equal(A("HorizontalAlignment") switch { "Right" => 0, "Center" => 1, _ => 2 }, field.Text.HorizontalAlignment);
        Assert.Equal(A("VerticalAlignment") switch { "Top" => 0, "Middle" => 1, _ => 2 }, field.Text.VerticalAlignment);
        AssertFont(tia.Descendants("Hmi.Globalization.FontItem").Single().Element("AttributeList")!, field.Font, step);
        return;

        string A(string name) => tia.Element("AttributeList")!.Element(name)!.Value;
    }

    /// <summary>
    /// A603A0097 (V21, TP1900 Comfort): 107 полей — 8 в VIEW_IN, 99 в VIEW_OUT (Text field и Symbolic I/O field VIEW_OUT
    /// — другой код типа, не поля), у каждого найден тег: у двух <c>IO Field_2</c> — через ADAPTVALUECHANG и VARLIST
    /// (<c>Step_Number</c>, как в TIA; типа нет); Units с текстом длиннее двух символов (<c>mm/s</c>) — ссылки на стили за
    /// ним не выровнены, поле читается; предупреждений нет. Все свойства 105 полей сверены с выгрузкой Openness
    /// A603A0097 (вне репозитория) скриптом, не этим тестом.
    /// </summary>
    [Fact]
    public void A603_FieldsWithoutWarnings()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/a603/pdata.fwc"));

        Assert.Empty(doc.Warnings);
        Assert.Equal(8, doc.IoFields.Count(f => f.Mode != IoFieldMode.Output));
        Assert.Equal(99, doc.IoFields.Count(f => f.Mode == IoFieldMode.Output));
        Assert.DoesNotContain(doc.IoFields, f => f.Tag is null || f.TagVia is not null);
        Assert.Equal(2, doc.IoFields.Count(f => f is { Tag: "Step_Number", TagType: null }));
        Assert.Contains(doc.IoFields, f => f.Unit == "mm/s");
    }

    /// <summary>
    /// Шаг 05, размер блока <c>I/O field_1</c> (VIEW_IN 1, +0x4c: имя 11 символов, R = 3, ссылки, +0x14 от их конца) —
    /// 0x100: метка стилей не на месте — предупреждение с таблицей и смещением, поля нет, объект на холсте остаётся
    /// (рамкой), другое поле читается.
    /// </summary>
    [Fact]
    public void BrokenBlock_WarningAndNoField()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U32(dir, "samples/screens/05_popup.fwc", "VIEW_IN", 0, 0x4c, 0x100);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"VIEW_IN @ 0x{item.Offset:X}: запись 1: объект I/O field_1: метка стилей I/O field не на месте"], doc.Warnings);
        Assert.Contains(doc.ScreenObjects, o => o.Name == "I/O field_1");
        Assert.DoesNotContain(doc.IoFields, f => f.Element.Index == 0 && doc.FindTable(f.Element.Table)!.Name == "VIEW_IN");
        Assert.Equal(3, doc.IoFields.Count);
    }

    /// <summary>
    /// Шаг 05, номер элемента PS_Fill_Color у <c>I/O field_1</c> (+0x86: третья ссылка на стили перед меткой на +0x88) —
    /// 0x7FFF: предупреждение с таблицей стиля и номером элемента, поля нет, другое поле читается.
    /// </summary>
    [Fact]
    public void BrokenStyleLink_WarningAndNoField()
    {
        using var dir = new TestDirectory();
        var (input, _) = CorruptedCopy.U16(dir, "samples/screens/05_popup.fwc", "VIEW_IN", 0, 0x86, 0x7FFF);
        var doc = FwxReader.Read(input);

        var warning = Assert.Single(doc.Warnings);
        Assert.StartsWith("PS_Fill_Color @ 0x", warning);
        Assert.EndsWith("объект I/O field_1 ссылается на элемент 32768, а элементов 8", warning);
        Assert.Equal(3, doc.IoFields.Count);
    }

    /// <summary>Шрифт против XML: размер, Bold, Italic, Underline; имя — кроме шага <see cref="FontMismatchStep"/>.</summary>
    private static void AssertFont(XElement font, PanelFont actual, string step)
    {
        var style = font.Element("FontStyle")!.Value;
        Assert.Equal(double.Parse(font.Element("FontSize")!.Value), actual.Size);
        Assert.Equal(style.Contains("Bold"), actual.Bold);
        Assert.Equal(style.Contains("Italic"), actual.Italic);
        Assert.Equal(style.Contains("Underline"), actual.Underline);
        Assert.Equal(step == FontMismatchStep ? "Tahoma" : font.Element("FontFamily")!.Value, actual.Name);
    }

    /// <summary>Тег поля в XML TIA: Property ProcessValue → TagConnectionDynamic → Tag.</summary>
    /// <returns>Имя тега; тега нет — <c>null</c>.</returns>
    private static string? Tag(XElement field) =>
        field.Descendants("Hmi.Screen.Property")
            .FirstOrDefault(p => p.Element("AttributeList")?.Element("Name")?.Value == "ProcessValue")
            ?.Descendants("Tag").Single().Element("Name")!.Value;

    /// <summary>Цвет из XML TIA <c>R, G, B</c>.</summary>
    /// <returns>Цвет <c>0xRRGGBB</c>.</returns>
    private static int Rgb(string text)
    {
        var parts = text.Split(',').Select(p => int.Parse(p.Trim())).ToArray();
        return parts[0] << 16 | parts[1] << 8 | parts[2];
    }

    /// <summary>Поле объекта с этим именем на экране.</summary>
    /// <returns>Поле.</returns>
    private static IoField Field(FwxDocument doc, string screen, string name)
    {
        var index = doc.Screens.Single(s => s.Name == screen).Index;
        var element = doc.ScreenObjects.Single(o => o.ScreenIndex == index && o.Name == name).Element;
        return doc.IoFields.Single(f => f.Element == element);
    }
}
