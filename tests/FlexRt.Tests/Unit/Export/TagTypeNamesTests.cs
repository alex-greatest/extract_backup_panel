using FlexRt.Export.Tags;
using FlexRt.Model.Panel;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Название типа HMI-тега <see cref="TagTypeNames.Format"/>. Ожидания — таблица «Коды типов»
/// в <c>docs/формат-fwx/таблица-var.md</c> и колонка Data type в
/// <c>samples/expected/теги.txt</c> и <c>samples/expected/plc/теги.txt</c>.
/// </summary>
public sealed class TagTypeNamesTests
{
    /// <summary>Скалярные коды из таблицы VAR.</summary>
    [Theory]
    [InlineData(0x02, "Int")]
    [InlineData(0x03, "DInt")]
    [InlineData(0x04, "Real")]
    [InlineData(0x05, "LReal")]
    [InlineData(0x07, "DateTime")]
    [InlineData(0x08, "WString")]
    [InlineData(0x0b, "Bool")]
    [InlineData(0x10, "SInt")]
    [InlineData(0x11, "USInt")]
    [InlineData(0x12, "UInt")]
    [InlineData(0x13, "UDInt")]
    public void Format_Scalar(int typeCode, string expected)
    {
        Assert.Equal(expected, TagTypeNames.Format(Tag(typeCode, 1)));
    }

    /// <summary>
    /// Массив — бит 0x2000, вывод <c>Array [0..N-1] of X</c>: теги с 23 элементами из
    /// <c>samples/pdata.fwc</c> и <c>samples/plc/pdata.fwc</c> (в том числе заданный в TIA
    /// как <c>[5..22]</c> — нижняя граница не хранится).
    /// </summary>
    [Theory]
    [InlineData(0x200b, 23, "Array [0..22] of Bool")]
    [InlineData(0x2005, 23, "Array [0..22] of LReal")]
    [InlineData(0x2002, 23, "Array [0..22] of Int")]
    [InlineData(0x2012, 23, "Array [0..22] of UInt")]
    public void Format_Array(int typeCode, int elements, string expected)
    {
        Assert.Equal(expected, TagTypeNames.Format(Tag(typeCode, elements)));
    }

    /// <summary>
    /// Неизвестный код, в том числе с другими старшими битами, — <c>?</c> (CLAUDE.md,
    /// «Установленное поведение»; документ таблицы VAR). Коды PLC-тегов (код HMI | 0x80,
    /// например 0x82) сюда не входят: они не «неизвестные», а у PLC-тега тип берётся не из
    /// <see cref="TagTypeNames"/> (<c>docs/формат-fwx/таблица-datalink-readwr.md</c>).
    /// </summary>
    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x06)]
    [InlineData(0x4002)]
    [InlineData(0x2001)]
    public void Format_UnknownCode_QuestionMark(int typeCode)
    {
        Assert.Equal("?", TagTypeNames.Format(Tag(typeCode, 1)));
    }

    /// <summary>Внутренний HMI-тег с заданным кодом типа и числом элементов.</summary>
    /// <param name="typeCode">Код типа как в VAR.</param>
    /// <param name="elements">Число элементов.</param>
    /// <returns>Тег без связи с ПЛК.</returns>
    private static HmiTag Tag(int typeCode, int elements) => new(1, "Tag", typeCode, elements, 0, 0, null, null);
}
