using FlexRt.Export;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Текст ячейки Excel <see cref="ExcelCell.Text"/>: строка длиннее 32767 символов
/// обрезается (CLAUDE.md, «Установленное поведение», раздел XLSX).
/// </summary>
public sealed class ExcelCellTests
{
    /// <summary>Предел длины текста ячейки Excel из CLAUDE.md.</summary>
    private const int Limit = 32767;

    /// <summary>Строка не длиннее предела возвращается без изменений.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(Limit - 1)]
    [InlineData(Limit)]
    public void Text_UpToLimit_Unchanged(int length)
    {
        var value = new string('a', length);

        Assert.Equal(value, ExcelCell.Text(value));
    }

    /// <summary>Строка длиннее предела обрезается до первых 32767 символов.</summary>
    [Theory]
    [InlineData(Limit + 1)]
    [InlineData(Limit * 2)]
    public void Text_OverLimit_TruncatedToPrefix(int length)
    {
        var value = string.Concat(Enumerable.Range(0, length).Select(i => (char)('a' + i % 26)));

        Assert.Equal(value[..Limit], ExcelCell.Text(value));
    }

    /// <summary>
    /// Текст, похожий на формулу, не меняется: <see cref="ExcelCell.Text"/> только обрезает.
    /// Защита от формулы — запись через <c>SetValue</c> в экспортёрах, не в этой функции.
    /// Фиксирует текущее поведение.
    /// </summary>
    [Fact]
    public void Text_FormulaLike_NotAltered()
    {
        Assert.Equal("=SUM(A1:A2)", ExcelCell.Text("=SUM(A1:A2)"));
    }
}
