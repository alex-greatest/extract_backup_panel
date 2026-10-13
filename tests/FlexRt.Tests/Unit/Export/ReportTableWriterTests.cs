using ClosedXML.Excel;
using FlexRt.Export;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Заливка ячеек <see cref="ReportTableWriter.Write"/>: ячейка с <see cref="ReportCell.FillRgb"/>
/// закрашивается этим цветом, на тёмной заливке (яркость 0.299·R + 0.587·G + 0.114·B ниже 64) текст
/// белый; ячейка без цвета не закрашивается.
/// </summary>
public sealed class ReportTableWriterTests
{
    /// <summary>
    /// Цвет заливки и цвет текста по яркости: красный (76), зелёный <c>0, 128, 0</c> (75), серый 65 и
    /// белый — текст не белый; серый 63, тёмно-синий и чёрный — белый.
    /// </summary>
    [Theory]
    [InlineData(0xFF0000, false)]
    [InlineData(0x008000, false)]
    [InlineData(0x414141, false)]
    [InlineData(0xFFFFFF, false)]
    [InlineData(0x3F3F3F, true)]
    [InlineData(0x000080, true)]
    [InlineData(0x000000, true)]
    public void Write_FillRgb_FillsCellAndWhitensTextOnDarkFill(int rgb, bool whiteText)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Лист");
        ReportCell cell = "цвет";

        ReportTableWriter.Write(sheet, 1, new ReportTable(["Цвет"], [[cell with { FillRgb = rgb }]]));

        var style = sheet.Cell(2, 1).Style;
        Assert.Equal("цвет", sheet.Cell(2, 1).GetString());
        Assert.Equal(XLFillPatternValues.Solid, style.Fill.PatternType);
        Assert.Equal(rgb, style.Fill.BackgroundColor.Color.ToArgb() & 0xFFFFFF);
        Assert.Equal(whiteText, style.Font.FontColor.ColorType == XLColorType.Color && (style.Font.FontColor.Color.ToArgb() & 0xFFFFFF) == 0xFFFFFF);
    }

    /// <summary>Ячейка без <see cref="ReportCell.FillRgb"/> не закрашивается.</summary>
    [Fact]
    public void Write_NoFillRgb_NoFill()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Лист");

        ReportTableWriter.Write(sheet, 1, new ReportTable(["Цвет"], [["цвета классов выключены"]]));

        Assert.Equal(XLFillPatternValues.None, sheet.Cell(2, 1).Style.Fill.PatternType);
    }
}
