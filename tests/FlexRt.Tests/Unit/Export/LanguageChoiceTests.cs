using FlexRt.Export;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Язык таблиц окна после открытия и после перечитывания панели (<see cref="LanguageChoice"/>): <c>null</c> — все языки.
/// </summary>
public sealed class LanguageChoiceTests
{
    /// <summary>Языки панели в порядке STRINGSTORE, как у <c>samples/a603/pdata.fwc</c>.</summary>
    private static readonly int[] Languages = [0x409, 0x419, 0x415];

    /// <summary>После открытия — первый язык панели; языков нет — все языки.</summary>
    [Fact]
    public void First_FirstLanguageOrAll()
    {
        Assert.Equal(0x409, LanguageChoice.First(Languages));
        Assert.Null(LanguageChoice.First([]));
    }

    /// <summary>После перечитывания выбранный язык остаётся, если он есть у панели; «Все языки» остаются.</summary>
    [Theory]
    [InlineData(0x419)]
    [InlineData(0x415)]
    [InlineData(null)]
    public void Kept_PresentOrAll_Stays(int? current)
    {
        Assert.Equal(current, LanguageChoice.Kept(Languages, current));
    }

    /// <summary>Выбранного языка у перечитанной панели нет — первый язык.</summary>
    [Fact]
    public void Kept_Missing_First()
    {
        Assert.Equal(0x409, LanguageChoice.Kept(Languages, 0x407));
    }
}
