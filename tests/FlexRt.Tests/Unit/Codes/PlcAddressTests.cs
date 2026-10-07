using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;
using Xunit;

namespace FlexRt.Tests.Unit.Codes;

/// <summary>Логический адрес абсолютного PLC-тега: <see cref="PlcAddress.Format"/>.</summary>
public sealed class PlcAddressTests
{
    /// <summary>
    /// Адреса тегов <c>samples/plc/pdata.fwc</c> с абсолютным доступом. Входные поля — разбор
    /// этого файла, ожидаемый адрес — колонка Address в <c>samples/expected/plc/теги.txt</c>
    /// (адреса из TIA; <c>%IW44</c>, <c>%I1.5</c>, <c>%T0</c> — ещё и CLAUDE.md).
    /// </summary>
    [Theory]
    [InlineData(PlcArea.Input, 1, 5, 1, "%I1.5")]
    [InlineData(PlcArea.Output, 0, 0, 1, "%Q0.0")]
    [InlineData(PlcArea.Memory, 0, 1, 1, "%M0.1")]
    [InlineData(PlcArea.Input, 2, 0, 8, "%IB2")]
    [InlineData(PlcArea.Memory, 12, 0, 16, "%MW12")]
    [InlineData(PlcArea.Input, 6, 0, 32, "%ID6")]
    [InlineData(PlcArea.Output, 50, 0, 32, "%QD50")]
    [InlineData(PlcArea.Input, 44, 0, 16, "%IW44")]
    [InlineData(PlcArea.Counter, 0, 0, 16, "%C0")]
    [InlineData(PlcArea.Timer, 0, 0, 16, "%T0")]
    public void Format_SampleAbsoluteTags_MatchesTia(PlcArea area, int byteOffset, int bit, int bitSize, string expected)
    {
        Assert.Equal(expected, PlcAddress.Format(PlcLinks.Absolute(area, byteOffset, bit, bitSize)));
    }

    /// <summary>
    /// Адрес в DB: <c>%DB80.DBW0</c> — CLAUDE.md, <c>%DB80.DBW46</c> — документ
    /// таблицы DATALINK_READWR (236 адресов A603A0097 совпали с проектом),
    /// <c>%DB80.DBX0.1</c> — XML-документация метода.
    /// </summary>
    [Theory]
    [InlineData(0, 0, 16, "%DB80.DBW0")]
    [InlineData(46, 0, 16, "%DB80.DBW46")]
    [InlineData(0, 1, 1, "%DB80.DBX0.1")]
    public void Format_DataBlock_UsesDbPrefix(int byteOffset, int bit, int bitSize, string expected)
    {
        Assert.Equal(expected, PlcAddress.Format(PlcLinks.Absolute(PlcArea.DataBlock, byteOffset, bit, bitSize, dbNumber: 80)));
    }

    /// <summary>
    /// 64-битное значение и массив — адрес начала с битом (<c>%I32.0</c>,
    /// <c>%DB81.DBX96.0</c>), как в XML-документации метода. Фиксирует текущее поведение,
    /// TIA не подтверждено: вид такого адреса в колонке HMI не проверялся.
    /// </summary>
    [Theory]
    [InlineData(PlcArea.Input, 0, 32, 64, 1, "%I32.0")]
    [InlineData(PlcArea.DataBlock, 81, 96, 32, 10, "%DB81.DBX96.0")]
    public void Format_WideOrArray_StartAddressWithBit(PlcArea area, int dbNumber, int byteOffset, int bitSize, int elements, string expected)
    {
        Assert.Equal(expected, PlcAddress.Format(PlcLinks.Absolute(area, byteOffset, 0, bitSize, elements, dbNumber)));
    }

    /// <summary>
    /// Символьный доступ — адреса нет (CLAUDE.md: «у символьного пусто, как в TIA»);
    /// неизвестная область — тоже <c>null</c> (XML-документация метода).
    /// </summary>
    [Fact]
    public void Format_SymbolicOrUnknownArea_ReturnsNull()
    {
        Assert.Null(PlcAddress.Format(PlcLinks.Symbolic(PlcArea.Input, 0x03)));
        Assert.Null(PlcAddress.Format(PlcLinks.Absolute(null, 44, 0, 16)));
    }
}
