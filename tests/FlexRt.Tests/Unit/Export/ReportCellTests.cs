using FlexRt.Export;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>Текст ячейки на выбранном языке для окна (<see cref="ReportCell.DisplayIn"/>).</summary>
public sealed class ReportCellTests
{
    /// <summary>Ячейка с текстами на языках: текст «Excel» — первый язык, тексты по LCID.</summary>
    private static readonly ReportCell Translated = ((ReportCell)"DISP") with
    {
        LanguageTexts = new Dictionary<int, string> { [0x409] = "DISP", [0x419] = "Alarm_class_1" }
    };

    /// <summary>Выбран язык, текст которого есть у ячейки, — этот текст.</summary>
    [Fact]
    public void DisplayIn_KnownLanguage_ItsText()
    {
        Assert.Equal("Alarm_class_1", Translated.DisplayIn(0x419));
    }

    /// <summary>Все языки (<c>null</c>) или языка нет у ячейки — текст ячейки, как в Excel.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(0x415)]
    public void DisplayIn_AllOrMissing_Display(int? language)
    {
        Assert.Equal("DISP", Translated.DisplayIn(language));
    }

    /// <summary>Ячейка без текстов на языках — <see cref="ReportCell.Display"/> при любом языке.</summary>
    [Fact]
    public void DisplayIn_Plain_Display()
    {
        Assert.Equal("Errors", ((ReportCell)"Errors").DisplayIn(0x419));
        Assert.Equal("7", ((ReportCell)7L).DisplayIn(0x419));
    }
}
