using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;
using Xunit;

namespace FlexRt.Tests.Unit.Codes;

/// <summary>
/// Коды типов ПЛК <see cref="PlcTypeCodes"/>. Ожидания — раздел «Коды типов» в
/// <c>docs/формат-fwx/таблица-datalink-readwr.md</c>: 0x08 String установлен на 12 тегах
/// A603A0097_V21, 0x0d Date_And_Time — наблюдение на одном элементе DATALINK того же
/// проекта (у тега VAR не встречался), тест фиксирует текущее поведение кода.
/// </summary>
public sealed class PlcTypeCodesTests
{
    /// <summary>Однозначные коды: один тип.</summary>
    [Theory]
    [InlineData(0x00, "SInt")]
    [InlineData(0x01, "Byte")]
    [InlineData(0x02, "Int")]
    [InlineData(0x03, "Word")]
    [InlineData(0x05, "DWord")]
    [InlineData(0x06, "Real")]
    [InlineData(0x07, "Bool")]
    [InlineData(0x08, "String")]
    [InlineData(0x0a, "Counter")]
    [InlineData(0x0b, "Date")]
    [InlineData(0x0c, "Time_Of_Day")]
    [InlineData(0x0d, "Date_And_Time")]
    [InlineData(0x0f, "LInt")]
    [InlineData(0x10, "ULInt")]
    [InlineData(0x12, "UInt")]
    [InlineData(0x13, "UDInt")]
    [InlineData(0x15, "LReal")]
    [InlineData(0x16, "LTime")]
    [InlineData(0x17, "LDT")]
    [InlineData(0x18, "LTime_Of_Day")]
    [InlineData(0x21, "WChar")]
    public void Candidates_SingleType(int code, string expected)
    {
        Assert.Equal([expected], PlcTypeCodes.Candidates(PlcLinks.Symbolic(PlcArea.Memory, code)));
    }

    /// <summary>Неразличимые пары: Char/USInt (0x11) и DInt/Time (0x04).</summary>
    [Theory]
    [InlineData(0x11, "USInt", "Char")]
    [InlineData(0x04, "DInt", "Time")]
    public void Candidates_IndistinguishablePair(int code, string first, string second)
    {
        Assert.Equal([first, second], PlcTypeCodes.Candidates(PlcLinks.Symbolic(PlcArea.Memory, code)));
    }

    /// <summary>
    /// Код 0x09: в области T — Timer (<c>Timer_T0</c>, <c>Timer_T1</c>), в I/Q/M — S5Time
    /// (<c>S5Time_IW132</c>, <c>S5Time_QW134</c>, <c>S5Time_MW136</c>).
    /// </summary>
    [Theory]
    [InlineData(PlcArea.Timer, "Timer")]
    [InlineData(PlcArea.Input, "S5Time")]
    [InlineData(PlcArea.Output, "S5Time")]
    [InlineData(PlcArea.Memory, "S5Time")]
    public void Candidates_S5TimeOrTimer_ByArea(PlcArea area, string expected)
    {
        Assert.Equal([expected], PlcTypeCodes.Candidates(PlcLinks.Symbolic(area, 0x09)));
    }

    /// <summary>Коды, которые не встречались (0x0e, 0x14, 0x19, 0x22), — пустой список.</summary>
    [Theory]
    [InlineData(0x0e)]
    [InlineData(0x14)]
    [InlineData(0x19)]
    [InlineData(0x22)]
    public void Candidates_UnknownCode_Empty(int code)
    {
        Assert.Empty(PlcTypeCodes.Candidates(PlcLinks.Symbolic(PlcArea.Memory, code)));
    }

    /// <summary>
    /// <see cref="PlcTypeCodes.IsKnown"/>: типы из таблицы кодов и Timer известны; LWord
    /// (панель его не принимает) и типы вне таблицы — нет.
    /// </summary>
    [Theory]
    [InlineData("Timer", true)]
    [InlineData("Char", true)]
    [InlineData("Time", true)]
    [InlineData("LTime_Of_Day", true)]
    [InlineData("LWord", false)]
    [InlineData("WString", false)]
    [InlineData("", false)]
    public void IsKnown_ByTable(string typeName, bool expected)
    {
        Assert.Equal(expected, PlcTypeCodes.IsKnown(typeName));
    }
}
