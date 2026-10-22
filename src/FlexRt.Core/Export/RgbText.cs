namespace FlexRt.Export;

/// <summary>Цвет <c>0xRRGGBB</c> в отчёте: текст <c>R, G, B</c>, как в свойствах TIA, и ячейка с заливкой этим цветом.</summary>
public static class RgbText
{
    /// <summary>Цвет <c>R &lt;&lt; 16 | G &lt;&lt; 8 | B</c> текстом.</summary>
    /// <returns><c>R, G, B</c> десятичными: <c>182, 182, 182</c>.</returns>
    private static string Format(int rgb) => $"{rgb >> 16 & 0xFF}, {rgb >> 8 & 0xFF}, {rgb & 0xFF}";

    /// <summary>Ячейка цвета <c>R &lt;&lt; 16 | G &lt;&lt; 8 | B</c>: текст <see cref="Format"/>, заливка — этот цвет.</summary>
    /// <returns>Ячейка с <see cref="ReportCell.FillRgb"/>.</returns>
    public static ReportCell Cell(int rgb) => (ReportCell)Format(rgb) with { FillRgb = rgb };
}
