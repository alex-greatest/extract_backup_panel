using FlexRt.Export;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>Код языка панели в виде для листа «Сводка» (<see cref="LanguageText"/>).</summary>
public sealed class LanguageTextTests
{
    /// <summary>Известные коды: код и название языка на нём самом — не зависит от языка Windows.</summary>
    [Theory]
    [InlineData(0x419, "0x419 — русский (Россия)")]
    [InlineData(0x409, "0x409 — English (United States)")]
    [InlineData(0x407, "0x407 — Deutsch (Deutschland)")]
    public void KnownLcid_CodeAndNativeName(int lcid, string expected)
    {
        Assert.Equal(expected, LanguageText.Format(lcid));
    }

    /// <summary>Код, которого .NET не знает, — только код, без ошибки.</summary>
    [Fact]
    public void UnknownLcid_CodeOnly()
    {
        Assert.Equal("0x7ffff", LanguageText.Format(0x7ffff));
    }
}
