using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;
using Xunit;

namespace FlexRt.Tests.Unit.Codes;

/// <summary>
/// Коды областей <see cref="PlcAreaCodes"/>. Ожидания — таблицы символьного (<c>+0x20</c>) и
/// абсолютного (<c>+0x1e</c>) доступа в <c>docs/формат-fwx/таблица-datalink-readwr.md</c>.
/// </summary>
public sealed class PlcAreaCodesTests
{
    /// <summary>Символьные коды I/Q/M/C/T: 0x50..0x54.</summary>
    [Theory]
    [InlineData(0x50L, PlcArea.Input)]
    [InlineData(0x51L, PlcArea.Output)]
    [InlineData(0x52L, PlcArea.Memory)]
    [InlineData(0x53L, PlcArea.Counter)]
    [InlineData(0x54L, PlcArea.Timer)]
    public void FromSymbolic_KnownCodes(long code, PlcArea expected)
    {
        Assert.Equal(expected, PlcAreaCodes.FromSymbolic(code));
        Assert.Equal(0, PlcAreaCodes.DbFromSymbolic(code));
    }

    /// <summary>
    /// Символьный код DB — <c>0x8a0e0000</c> + номер DB: <c>0x8a0e0038</c> — DB56
    /// (XML-документация константы), <c>0x8a0e0050</c> — DB80 (то же правило).
    /// </summary>
    [Theory]
    [InlineData(0x8a0e0038L, 56)]
    [InlineData(0x8a0e0050L, 80)]
    public void FromSymbolic_DataBlock_AreaAndNumber(long code, int db)
    {
        Assert.Equal(PlcArea.DataBlock, PlcAreaCodes.FromSymbolic(code));
        Assert.Equal(db, PlcAreaCodes.DbFromSymbolic(code));
    }

    /// <summary>
    /// Код, который не встречался, — <c>null</c>, номер DB 0. Фиксирует текущее поведение,
    /// TIA не подтверждено (такие коды в файлах не наблюдались).
    /// </summary>
    [Theory]
    [InlineData(0x00L)]
    [InlineData(0x55L)]
    [InlineData(0x8a0f0038L)]
    public void FromSymbolic_UnknownCode_ReturnsNull(long code)
    {
        Assert.Null(PlcAreaCodes.FromSymbolic(code));
        Assert.Equal(0, PlcAreaCodes.DbFromSymbolic(code));
    }

    /// <summary>Абсолютные коды: 00 DB, 02 M, 04 I, 05 Q, 06 C, 07 T.</summary>
    [Theory]
    [InlineData(0x00L, PlcArea.DataBlock)]
    [InlineData(0x02L, PlcArea.Memory)]
    [InlineData(0x04L, PlcArea.Input)]
    [InlineData(0x05L, PlcArea.Output)]
    [InlineData(0x06L, PlcArea.Counter)]
    [InlineData(0x07L, PlcArea.Timer)]
    public void FromAbsolute_KnownCodes(long code, PlcArea expected)
    {
        Assert.Equal(expected, PlcAreaCodes.FromAbsolute(code));
    }

    /// <summary>
    /// Абсолютный код вне таблицы — <c>null</c>. Фиксирует текущее поведение, TIA не
    /// подтверждено.
    /// </summary>
    [Theory]
    [InlineData(0x01L)]
    [InlineData(0x03L)]
    [InlineData(0x50L)]
    public void FromAbsolute_UnknownCode_ReturnsNull(long code)
    {
        Assert.Null(PlcAreaCodes.FromAbsolute(code));
    }
}
