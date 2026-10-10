using ClosedXML.Excel;
using FlexRt.Export;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Текст ячейки Excel <see cref="ExcelCell.Text"/>: строка длиннее 32767 символов
/// обрезается, апостроф в начале сохраняется (CLAUDE.md, «Установленное поведение», раздел XLSX).
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

    /// <summary>
    /// Текст с апострофом в начале получает второй апостроф: ClosedXML при записи снимает
    /// первый как префикс текста Excel.
    /// </summary>
    [Fact]
    public void Text_LeadingApostrophe_Doubled()
    {
        Assert.Equal("''Project ID' area pointer", ExcelCell.Text("'Project ID' area pointer"));
    }

    /// <summary>
    /// Текст, записанный через <c>SetValue(ExcelCell.Text(...))</c>, читается из ячейки без
    /// изменений: апостроф в начале, апостроф в середине, перенос строки, пробел в конце.
    /// </summary>
    [Theory]
    [InlineData("'Project ID' area pointer: Unknown error.")]
    [InlineData("''")]
    [InlineData("'")]
    [InlineData("Project ID' area pointer")]
    [InlineData("File %1 already exists.\nOverwrite file?")]
    [InlineData("Set default values for recipe. ")]
    public void Text_WrittenToCell_ReadBackUnchanged(string value)
    {
        using var workbook = new XLWorkbook();
        var cell = workbook.Worksheets.Add("Лист").Cell(1, 1);

        cell.SetValue(ExcelCell.Text(value));

        Assert.Equal(value, cell.GetString());
    }

    /// <summary>
    /// Длинный текст с апострофом в начале записывается в ячейку без исключения ClosedXML
    /// (предел 32767 проверяется до снятия апострофа): в ячейке — первые 32766 символов.
    /// </summary>
    [Theory]
    [InlineData(Limit - 1)]
    [InlineData(Limit)]
    [InlineData(Limit + 1)]
    public void Text_LongWithLeadingApostrophe_WrittenToCell(int length)
    {
        var value = "'" + new string('a', length - 1);
        using var workbook = new XLWorkbook();
        var cell = workbook.Worksheets.Add("Лист").Cell(1, 1);

        cell.SetValue(ExcelCell.Text(value));

        Assert.Equal(value[..(Limit - 1)], cell.GetString());
    }
}
