using System.Globalization;
using FlexRt.Export;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Код языка панели и его название на нём самом (<see cref="LanguageText.Format"/>): <c>0x419 — русский (Россия)</c> на
/// Windows. Название берётся из данных культур ОС (ICU или NLS), поэтому тест сверяет его с
/// <see cref="CultureInfo.NativeName"/> этой же машины, а не с текстом конкретной версии ICU; без данных культур
/// (<c>InvariantGlobalization</c>) — только код.
/// </summary>
public sealed class LanguageTextTests
{
    /// <summary>Известный код — <c>0x&lt;код&gt; — &lt;название на самом языке&gt;</c>; данных культур нет — только код.</summary>
    [Theory]
    [InlineData(0x419)]
    [InlineData(0x409)]
    [InlineData(0x407)]
    public void KnownLcid_CodeAndNativeName(int lcid)
    {
        Assert.Equal(NativeName(lcid) is { } name ? $"0x{lcid:x} — {name}" : $"0x{lcid:x}", LanguageText.Format(lcid));
    }

    /// <summary>Название не зависит от языка интерфейса, на котором запущена программа.</summary>
    [Fact]
    public void Name_IndependentOfUiCulture()
    {
        var saved = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var invariant = LanguageText.Format(0x419);
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal(invariant, LanguageText.Format(0x419));
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = saved;
        }
    }

    /// <summary>Код, который .NET не знает, — только код.</summary>
    [Fact]
    public void UnknownLcid_CodeOnly()
    {
        Assert.Equal("0x7ffff", LanguageText.Format(0x7ffff));
    }

    /// <summary>Название культуры на ней самой из данных ОС.</summary>
    /// <returns>Название; культура этой машине неизвестна — <c>null</c>.</returns>
    private static string? NativeName(int lcid)
    {
        try
        {
            return CultureInfo.GetCultureInfo(lcid).NativeName;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
