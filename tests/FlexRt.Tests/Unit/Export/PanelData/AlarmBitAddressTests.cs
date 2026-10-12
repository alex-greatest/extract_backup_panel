using FlexRt.Export.PanelData;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using Xunit;

namespace FlexRt.Tests.Unit.Export.PanelData;

/// <summary>
/// Адрес бита тега аларма <see cref="AlarmBitAddress.Format"/>. Ожидания — Trigger address и HMI
/// acknowledgment address из TIA на A603A0097 (TIA V21): теги <c>COM_ALARMS\DB_ALARM_WORD0</c>
/// (<c>%DB80.DBW0</c>), <c>T_ARR</c> (Array [0..3] of Word, <c>%DB80.DBX60.0</c>),
/// <c>DB_ST7_HMI_MANU_Axis_Parameter_DEC</c> и <c>T_500ms_1</c> (символьный Array [0..5] of Int).
/// </summary>
public sealed class AlarmBitAddressTests
{
    /// <summary>Id таблицы DATALINK_READWR в файлах TIA.</summary>
    private const int DatalinkReadWrId = 0x35;

    /// <summary>Код типа Int у HMI-тега.</summary>
    private const int IntType = 0x02;

    /// <summary>Абсолютный Int <c>%DB80.DBW0</c>: биты 0..7 — в байте 1, биты 8..15 — в байте 0.</summary>
    [Theory]
    [InlineData(0, "%DB80.DBX1.0")]
    [InlineData(7, "%DB80.DBX1.7")]
    [InlineData(8, "%DB80.DBX0.0")]
    [InlineData(15, "%DB80.DBX0.7")]
    public void Format_AbsoluteWord(int bit, string expected)
    {
        var match = new PlcMatch(PlcLookup.AddressOnly, "PLC", null, PlcLinks.Absolute(PlcArea.DataBlock, 0, 0, 16, dbNumber: 80));

        Assert.Equal(expected, AlarmBitAddress.Format(Tag(IntType), match, bit));
    }

    /// <summary>Абсолютный массив Word <c>%DB80.DBX60.0</c>: бит считается по словам массива.</summary>
    [Theory]
    [InlineData(3, "%DB80.DBX61.3")]
    [InlineData(12, "%DB80.DBX60.4")]
    [InlineData(20, "%DB80.DBX63.4")]
    [InlineData(63, "%DB80.DBX66.7")]
    public void Format_AbsoluteArray(int bit, string expected)
    {
        var match = new PlcMatch(PlcLookup.AddressOnly, "PLC", null, PlcLinks.Absolute(PlcArea.DataBlock, 60, 0, 64, elements: 4, dbNumber: 80));

        Assert.Equal(expected, AlarmBitAddress.Format(Tag(HmiTag.ArrayFlag | 0x92), match, bit));
    }

    /// <summary>Символьный тег, найден в ПЛК: имя тега ПЛК и <c>.x</c> с битом.</summary>
    [Fact]
    public void Format_SymbolicFound()
    {
        var plcTag = new PlcTag("DB_ST7_HMI_MANU.Axis.Parameter.DEC", 1, "Int", "", PlcArea.DataBlock, 0, "");
        var match = new PlcMatch(PlcLookup.Found, "PLC", plcTag, PlcLinks.Symbolic(PlcArea.DataBlock, 0x05));

        Assert.Equal("DB_ST7_HMI_MANU.Axis.Parameter.DEC.x7", AlarmBitAddress.Format(Tag(IntType), match, 7));
    }

    /// <summary>Символьный массив: номер элемента и бит в нём.</summary>
    [Theory]
    [InlineData(11, "Data_block_1.11[0].x11")]
    [InlineData(20, "Data_block_1.11[1].x4")]
    public void Format_SymbolicArray(int bit, string expected)
    {
        var plcTag = new PlcTag("Data_block_1.11", 1, "Array[0..5] of Int", "", PlcArea.DataBlock, 0, "");
        var match = new PlcMatch(PlcLookup.Found, "PLC", plcTag, PlcLinks.Symbolic(PlcArea.DataBlock, 0x05, elements: 6));

        Assert.Equal(expected, AlarmBitAddress.Format(Tag(HmiTag.ArrayFlag | IntType), match, bit));
    }

    /// <summary>Символьный тег без данных ПЛК — «неизвестно»; не найден в ПЛК — «отсутствует в файле ПЛК».</summary>
    [Fact]
    public void Format_SymbolicWithoutPlcData()
    {
        var link = PlcLinks.Symbolic(PlcArea.DataBlock, 0x05);

        Assert.Equal("неизвестно", AlarmBitAddress.Format(Tag(IntType), new PlcMatch(PlcLookup.Unknown, null, null, link), 3));
        Assert.Equal("отсутствует в файле ПЛК", AlarmBitAddress.Format(Tag(IntType), new PlcMatch(PlcLookup.NotInPlcFile, "PLC", null, link), 3));
        Assert.Equal("неизвестно", AlarmBitAddress.Format(Tag(IntType), null, 3));
    }

    /// <summary>Внутренний тег: адреса нет.</summary>
    [Fact]
    public void Format_InternalTag_Empty()
    {
        var tag = new HmiTag(1, "Tag", IntType, 1, 0, 0, null, null);

        Assert.Equal("", AlarmBitAddress.Format(tag, null, 0));
    }

    /// <summary>PLC-тег с кодом типа <paramref name="typeCode"/>.</summary>
    /// <returns>Тег, связанный через DATALINK_READWR.</returns>
    private static HmiTag Tag(int typeCode) => new(1, "Tag", typeCode, 1, 0, 0, DatalinkReadWrId, 0);
}
