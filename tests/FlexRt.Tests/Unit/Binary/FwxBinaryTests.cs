using FlexRt.Binary;
using Xunit;

namespace FlexRt.Tests.Unit.Binary;

/// <summary>
/// Примитивы чтения <see cref="FwxBinary"/>: little endian, строки, hex и проверка границ.
/// Ожидания — XML-документация методов и CLAUDE.md («Выход за границы файла даёт
/// FwxFormatException с секцией и смещением»).
/// </summary>
public sealed class FwxBinaryTests
{
    /// <summary>Байты <c>ef be 0c 00</c> — пример из XML-документации <see cref="FwxBinary.ToHex"/>.</summary>
    private static readonly byte[] Sample = [0xef, 0xbe, 0x0c, 0x00];

    /// <summary>D1 — байт без знака, D2 и D4 — little endian без знака.</summary>
    [Fact]
    public void D1_D2_D4_LittleEndianUnsigned()
    {
        var b = new FwxBinary(Sample);

        Assert.Equal(0xef, b.D1(0));
        Assert.Equal(0xbeef, b.D2(0));
        Assert.Equal(0x0cbe, b.D2(1));
        Assert.Equal(0x000cbeefL, b.D4(0));
    }

    /// <summary>D4 со старшим битом — положительное значение без знака, а не отрицательное.</summary>
    [Fact]
    public void D4_HighBit_Unsigned()
    {
        var b = new FwxBinary([0xff, 0xff, 0xff, 0xff]);

        Assert.Equal(0xFFFFFFFFL, b.D4(0));
    }

    /// <summary>
    /// Чтение за концом или до начала файла — <see cref="FwxFormatException"/> с секцией и
    /// смещением чтения; текст — «секция @ 0xСМЕЩЕНИЕ: …».
    /// </summary>
    [Theory]
    [InlineData(4L, 1)]
    [InlineData(3L, 2)]
    [InlineData(1L, 4)]
    [InlineData(-1L, 1)]
    public void Read_OutOfBounds_ThrowsWithSectionAndOffset(long pos, int size)
    {
        var b = new FwxBinary(Sample) { Section = "VAR" };

        var error = Assert.Throws<FwxFormatException>(() => Read(b, pos, size));

        Assert.Equal("VAR", error.Section);
        Assert.Equal(pos, error.Offset);
        Assert.StartsWith($"VAR @ 0x{pos:X}: ", error.Message);
    }

    /// <summary>Отрицательная длина диапазона — ошибка формата, секция по умолчанию <c>HEADER</c>.</summary>
    [Fact]
    public void Check_NegativeLength_Throws()
    {
        var error = Assert.Throws<FwxFormatException>(() => new FwxBinary(Sample).Check(0, -1));

        Assert.Equal("HEADER", error.Section);
    }

    /// <summary>Диапазон ровно до конца файла допустим.</summary>
    [Fact]
    public void Check_RangeEndingAtFileEnd_Ok()
    {
        var b = new FwxBinary(Sample);

        b.Check(0, 4);
        b.Check(4, 0);
        Assert.Equal(4, b.Length);
    }

    /// <summary>
    /// <see cref="FwxBinary.GetName"/>: длина в символах в первом слове, затем UTF-16LE —
    /// раскладка имени из документа таблицы VAR (<c>07 00</c> + <c>"Tag_Int"</c>).
    /// </summary>
    [Fact]
    public void GetName_LengthPrefixedUtf16()
    {
        byte[] data = [0x07, 0x00, 0x54, 0x00, 0x61, 0x00, 0x67, 0x00, 0x5f, 0x00, 0x49, 0x00, 0x6e, 0x00, 0x74, 0x00];
        var b = new FwxBinary(data);

        Assert.Equal("Tag_Int", b.GetName(0));
        Assert.Equal("Tag", b.GetNameLen(2, 3));
    }

    /// <summary>Длина имени 0 или меньше — пустая строка без чтения.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetNameLen_NonPositive_Empty(int chars)
    {
        Assert.Equal("", new FwxBinary([]).GetNameLen(100, chars));
    }

    /// <summary>Имя, которое выходит за конец файла, — ошибка формата.</summary>
    [Fact]
    public void GetNameLen_PastEnd_Throws()
    {
        Assert.Throws<FwxFormatException>(() => new FwxBinary([.. "A\0"u8]).GetNameLen(0, 2));
    }

    /// <summary>UTF-8 по смещению и длине, в том числе кириллица; выход за конец — ошибка.</summary>
    [Fact]
    public void GetUtf8_DecodesAndChecksBounds()
    {
        byte[] data = [0x0a, 0xd0, 0xa6, 0xd0, 0xb5, 0xd1, 0x85];
        var b = new FwxBinary(data);

        Assert.Equal("Цех", b.GetUtf8(1, 6));
        Assert.Throws<FwxFormatException>(() => b.GetUtf8(1, 7));
    }

    /// <summary>Span возвращает байты диапазона; выход за конец — ошибка.</summary>
    [Fact]
    public void Span_ReturnsRangeAndChecksBounds()
    {
        var b = new FwxBinary(Sample);

        Assert.Equal(new byte[] { 0xbe, 0x0c }, b.Span(1, 2).ToArray());
        Assert.Throws<FwxFormatException>(() => b.Span(3, 2).ToArray());
    }

    /// <summary>ToHex: двузначный hex в нижнем регистре через пробел (XML-документация).</summary>
    [Fact]
    public void ToHex_LowercasePairsSeparatedBySpace()
    {
        var b = new FwxBinary(Sample);

        Assert.Equal("ef be 0c 00", b.ToHex(0, 4));
        Assert.Equal("", b.ToHex(2, 0));
        Assert.Throws<FwxFormatException>(() => b.ToHex(2, 3));
    }

    /// <summary>Прочитать примитив размера 1, 2 или 4 байта.</summary>
    /// <param name="b">Байты.</param>
    /// <param name="pos">Смещение.</param>
    /// <param name="size">Размер примитива.</param>
    /// <returns>Прочитанное значение.</returns>
    private static long Read(FwxBinary b, long pos, int size) => size switch
    {
        1 => b.D1(pos),
        2 => b.D2(pos),
        _ => b.D4(pos)
    };
}
