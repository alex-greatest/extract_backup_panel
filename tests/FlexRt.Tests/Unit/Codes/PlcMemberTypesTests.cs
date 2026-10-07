using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;
using Xunit;

namespace FlexRt.Tests.Unit.Codes;

/// <summary>
/// Тип члена DB <see cref="PlcMemberTypes.Of"/>. Ожидания — раздел «XML» в
/// <c>docs/формат-plf/блоки-данных.md</c>: <c>RID = 0x020000TT</c>, TT 01 Bool, 02 Byte,
/// 04 Word, 05 Int, 08 Real, 13 String, 34 USInt, 35 UInt.
/// </summary>
public sealed class PlcMemberTypesTests
{
    /// <summary>Скаляр без атрибута <c>Type</c>: тип по <c>RID</c>.</summary>
    [Theory]
    [InlineData(0x02000001L, "Bool")]
    [InlineData(0x02000002L, "Byte")]
    [InlineData(0x02000004L, "Word")]
    [InlineData(0x02000005L, "Int")]
    [InlineData(0x02000008L, "Real")]
    [InlineData(0x02000013L, "String")]
    [InlineData(0x02000034L, "USInt")]
    [InlineData(0x02000035L, "UInt")]
    public void Of_ScalarRid(long rid, string expected)
    {
        Assert.Equal(expected, PlcMemberTypes.Of(Member("", rid)));
    }

    /// <summary>
    /// <c>RID 0x02000007</c> — DInt. Фиксирует текущее поведение, TIA не подтверждено:
    /// по документу «07 DInt — по одному источнику».
    /// </summary>
    [Fact]
    public void Of_DIntRid_SingleSource()
    {
        Assert.Equal("DInt", PlcMemberTypes.Of(Member("", 0x02000007)));
    }

    /// <summary>
    /// Атрибут <c>Type</c> есть — он и возвращается как есть (<c>Array[1..10] of Real</c>,
    /// <c>String[30]</c>, UDT в кавычках — примеры из XML-документации модели).
    /// </summary>
    [Theory]
    [InlineData("Array[1..10] of Real")]
    [InlineData("String[30]")]
    [InlineData("\"UDT_X\"")]
    public void Of_TypeAttribute_WinsOverRid(string type)
    {
        Assert.Equal(type, PlcMemberTypes.Of(Member(type, 0x02000001)));
    }

    /// <summary>
    /// Неизвестный код, не скалярный <c>RID</c> и отсутствующий <c>RID</c> (-1) — пустая
    /// строка, без догадок (XML-документация метода).
    /// </summary>
    [Theory]
    [InlineData(0x02000003L)]
    [InlineData(0x03000001L)]
    [InlineData(-1L)]
    public void Of_UnknownRid_Empty(long rid)
    {
        Assert.Equal("", PlcMemberTypes.Of(Member("", rid)));
    }

    /// <summary>Член DB с заданными <c>Type</c> и <c>RID</c>; остальные поля не влияют на тип.</summary>
    /// <param name="type">Атрибут <c>Type</c>.</param>
    /// <param name="rid">Атрибут <c>RID</c>.</param>
    /// <returns>Член DB.</returns>
    private static PlcDbMember Member(string type, long rid) => new("1", "Member", type, rid, 9);
}
