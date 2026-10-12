using FlexRt.Export.PanelData;
using Xunit;

namespace FlexRt.Tests.Unit.Export.PanelData;

/// <summary>
/// Подстановка полей в текст аларма <see cref="AlarmText.Format"/>. Метки — как в STRINGSTORE
/// A603A0097 (аларм 30: <c>No.:</c>, метка поля 1, пробел, метка поля 2); подписи — как в TIA.
/// </summary>
public sealed class AlarmTextTests
{
    /// <summary>Подписи полей аларма 30.</summary>
    private static readonly string[] Labels = ["<Tag : 6, DB_ST7_Axis_Axis_Receive_FAULT_CODE>", "<Text list, ?>"];

    /// <summary>Метки полей заменяются подписями по номеру поля.</summary>
    [Fact]
    public void Format_ReplacesMarks()
    {
        const string text = "Drive fault - Axis #1\nNo.:\eT\u0001ÿ \eT\u0002ÿ";

        Assert.Equal("Drive fault - Axis #1\nNo.:<Tag : 6, DB_ST7_Axis_Axis_Receive_FAULT_CODE> <Text list, ?>", AlarmText.Format(text, Labels));
    }

    /// <summary>Метка с номером, которого нет среди полей, — <c>&lt;?&gt;</c>; текст без меток не меняется.</summary>
    [Fact]
    public void Format_UnknownMarkAndPlainText()
    {
        Assert.Equal("a <?> b", AlarmText.Format("a \eT\u0003ÿ b", Labels));
        Assert.Equal("Alarm 5", AlarmText.Format("Alarm 5", Labels));
    }
}
