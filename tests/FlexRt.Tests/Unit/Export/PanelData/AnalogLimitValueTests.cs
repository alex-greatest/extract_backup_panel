using FlexRt.Export.PanelData.Alarms;
using Xunit;

namespace FlexRt.Tests.Unit.Export.PanelData;

/// <summary>
/// Значение предела-константы <see cref="AnalogLimitValue.Format"/>. Байты — как в MSG_ANALOG_OBSE
/// шаговой копии A603A0097: 50 на теге Int (вид 1), 13 и 12.5 на теге Real (вид 2), 555 на теге
/// UInt (вид 0).
/// </summary>
public sealed class AnalogLimitValueTests
{
    /// <summary>Вид 1 — целое со знаком числом, в том числе отрицательное из u32.</summary>
    [Fact]
    public void Signed_Number()
    {
        Assert.Equal(50L, AnalogLimitValue.Format(1, 0x32).Number);
        Assert.Equal(-5L, AnalogLimitValue.Format(1, 0xfffffffb).Number);
    }

    /// <summary>Вид 0 — целое без знака числом.</summary>
    [Fact]
    public void Unsigned_Number()
    {
        Assert.Equal(555L, AnalogLimitValue.Format(0, 0x22b).Number);
        Assert.Equal(0xfffffffbL, AnalogLimitValue.Format(0, 0xfffffffb).Number);
    }

    /// <summary>Вид 2 — float32 текстом в инвариантной культуре, как в TIA: <c>13</c>, <c>12.5</c>.</summary>
    [Fact]
    public void Float_InvariantText()
    {
        Assert.Equal("13", AnalogLimitValue.Format(2, 0x41500000).Text);
        Assert.Equal("12.5", AnalogLimitValue.Format(2, 0x41480000).Text);
    }

    /// <summary>Вид 3 — float64 текстом в инвариантной культуре (Project1: тег LReal, <c>1.5</c>).</summary>
    [Fact]
    public void Float64_InvariantText()
    {
        Assert.Equal("1.5", AnalogLimitValue.Format(3, 0x3ff8000000000000).Text);
    }

    /// <summary>Незнакомый вид — <c>?</c>.</summary>
    [Fact]
    public void UnknownKind_Question()
    {
        Assert.Equal("?", AnalogLimitValue.Format(4, 7).Text);
    }
}
