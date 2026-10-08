using FlexRt.Binary;
using FlexRt.Parsing.Plc.Plf;
using Xunit;

namespace FlexRt.Tests.Unit.Plc.Plf;

/// <summary>
/// Числа переменной длины, строки и мультиязычные тексты <see cref="PlfText"/>. Ожидания —
/// раздел «Строки и тексты» в <c>docs/формат-plf/тег-плк.md</c> и раздел «Строки и сжатие»
/// в <c>docs/формат-plf/общая-структура.md</c>.
/// </summary>
public sealed class PlfTextTests
{
    /// <summary>Секция в сообщениях об ошибках PEData.plf.</summary>
    private const string Section = "PEData.plf";

    /// <summary>Varint: один байт и пример <c>e5 03</c> = 485 из документа.</summary>
    [Theory]
    [InlineData(new byte[] { 0x0a }, 10L, 1L)]
    [InlineData(new byte[] { 0x00 }, 0L, 1L)]
    [InlineData(new byte[] { 0x7f }, 127L, 1L)]
    [InlineData(new byte[] { 0xe5, 0x03 }, 485L, 2L)]
    [InlineData(new byte[] { 0x80, 0x01 }, 128L, 2L)]
    public void ReadVarint_Values(byte[] data, long value, long next)
    {
        Assert.Equal((value, next), PlfText.ReadVarint(new FwxBinary(data), 0));
    }

    /// <summary>
    /// Varint из 5 байт — наибольший принимаемый (u32). Фиксирует текущее поведение, TIA не
    /// подтверждено: в документе varint длиннее 2 байт не встречались.
    /// </summary>
    [Fact]
    public void ReadVarint_FiveBytes_MaxUInt32()
    {
        byte[] data = [0xff, 0xff, 0xff, 0xff, 0x0f];

        Assert.Equal((0xFFFFFFFFL, 5L), PlfText.ReadVarint(new FwxBinary(data), 0));
    }

    /// <summary>Varint длиннее 5 байт — ошибка формата с секцией и смещением начала числа.</summary>
    [Fact]
    public void ReadVarint_TooLong_Throws()
    {
        byte[] data = [0x00, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01];

        var error = Assert.Throws<FwxFormatException>(() => PlfText.ReadVarint(new FwxBinary(data), 1));

        Assert.Equal(Section, error.Section);
        Assert.Equal(1, error.Offset);
    }

    /// <summary>Varint, обрезанный концом файла, — ошибка формата.</summary>
    [Fact]
    public void ReadVarint_TruncatedAtEnd_Throws()
    {
        Assert.Throws<FwxFormatException>(() => PlfText.ReadVarint(new FwxBinary([0x80]), 0));
    }

    /// <summary>Строка «длина + 1», затем UTF-8: пример <c>0a "Bool_I0_0"</c> из документа.</summary>
    [Fact]
    public void ReadString_Example()
    {
        byte[] data = [0x0a, .. "Bool_I0_0"u8];

        Assert.Equal(("Bool_I0_0", 10L), PlfText.ReadString(new FwxBinary(data), 0, data.Length));
    }

    /// <summary>Префикс 0 (строки нет) и 1 (пустая) — пустая строка, смещение за префиксом.</summary>
    [Theory]
    [InlineData((byte)0x00)]
    [InlineData((byte)0x01)]
    public void ReadString_AbsentOrEmpty(byte prefix)
    {
        Assert.Equal(("", 1L), PlfText.ReadString(new FwxBinary([prefix]), 0, 1));
    }

    /// <summary>Строка за границей объекта <c>end</c> — ошибка формата, даже если байты в файле есть.</summary>
    [Fact]
    public void ReadString_PastObjectEnd_Throws()
    {
        byte[] data = [0x04, 0x41, 0x42, 0x43, 0x44];

        var error = Assert.Throws<FwxFormatException>(() => PlfText.ReadString(new FwxBinary(data), 0, 3));

        Assert.Equal(Section, error.Section);
        Assert.Equal(0, error.Offset);
    }

    /// <summary>
    /// Пустой мультиязычный текст — 33 байта: пример из документа
    /// (<c>21 20 00 00 00 10 00 00 00 ff ff ff ff 00 00 00 00</c> + 16 байт хвоста).
    /// </summary>
    [Fact]
    public void ReadMultilingual_EmptyExample()
    {
        var data = EmptyMultilingual();

        Assert.Equal(("", 33L), PlfText.ReadMultilingual(new FwxBinary(data), 0, data.Length));
    }

    /// <summary>
    /// Один язык (0x409) с текстом, по грамматике документа: первое смещение текста
    /// <c>16 + 6n</c> от поля A; возвращается текст и смещение за блоком.
    /// </summary>
    [Fact]
    public void ReadMultilingual_OneLanguage_ReturnsText()
    {
        var data = OneLanguageMultilingual();

        Assert.Equal(("abc", 46L), PlfText.ReadMultilingual(new FwxBinary(data), 0, data.Length));
    }

    /// <summary>Блок длиннее границы объекта или с неверным полем A − 16 — ошибка формата.</summary>
    [Fact]
    public void ReadMultilingual_BadLayout_Throws()
    {
        var data = EmptyMultilingual();
        Assert.Throws<FwxFormatException>(() => PlfText.ReadMultilingual(new FwxBinary(data), 0, data.Length - 1));

        data[5] = 0x11;
        Assert.Throws<FwxFormatException>(() => PlfText.ReadMultilingual(new FwxBinary(data), 0, data.Length));
    }

    /// <summary>
    /// <see cref="PlfText.Attempt{T}"/>: ошибка формата превращается в <c>default</c>,
    /// успешное чтение возвращает результат.
    /// </summary>
    [Fact]
    public void Attempt_FormatErrorGivesDefault()
    {
        var b = new FwxBinary([0x05]);

        Assert.Equal(5, PlfText.Attempt(() => (int?)b.D1(0)));
        Assert.Null(PlfText.Attempt(() => (int?)b.D1(1)));
    }

    /// <summary>Пустой мультиязычный текст из документа <c>тег-плк.md</c>.</summary>
    /// <returns>33 байта блока.</returns>
    private static byte[] EmptyMultilingual() =>
        [0x21, 0x20, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, .. new byte[16]];

    /// <summary>
    /// Мультиязычный текст с одним языком по грамматике документа: varint X = 0x2e, A = 0x2d,
    /// A − 16 = 0x1d, <c>ff ff ff ff</c>, n = 1, LCID 0x409, смещение 0x16, текст
    /// <c>[u32 3]"abc"</c>, 16 байт хвоста.
    /// </summary>
    /// <returns>46 байт блока.</returns>
    private static byte[] OneLanguageMultilingual() =>
    [
        0x2e,
        0x2d, 0x00, 0x00, 0x00,
        0x1d, 0x00, 0x00, 0x00,
        0xff, 0xff, 0xff, 0xff,
        0x01, 0x00, 0x00, 0x00,
        0x09, 0x04,
        0x16, 0x00, 0x00, 0x00,
        0x03, 0x00, 0x00, 0x00, .. "abc"u8,
        .. new byte[16]
    ];
}
