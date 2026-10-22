using FlexRt.Export;

namespace FlexRt.Ui.Components.Sections;

/// <summary>Строка таблицы раздела текстом для буфера обмена: при вставке в Excel ложится в одну строку по ячейкам.</summary>
public static class ClipboardRow
{
    /// <summary>Символы, из-за которых Excel разорвал бы ячейку или строку при вставке: табуляция, CR, LF.</summary>
    private static readonly char[] Breaks = ['\t', '\r', '\n'];

    /// <summary>
    /// Ячейки строки через табуляцию (<see cref="ReportCell.Display"/>). В кавычки, как пишет Excel, берётся только ячейка с
    /// табуляцией, CR или LF либо ячейка, которая начинается с кавычки: <c>"Line1\r\nLine2"</c>; кавычки внутри такой ячейки
    /// удвоены. Остальные ячейки — как есть, и кавычки в середине тоже: <c>Valve "A" open</c>.
    /// </summary>
    /// <returns>Текст строки для буфера обмена.</returns>
    public static string Text(IEnumerable<ReportCell> row) => string.Join('\t', row.Select(c => Quoted(c.Display)));

    /// <summary>
    /// Текст ячейки для буфера: с табуляцией, переносом строки или кавычкой в начале — в кавычках с удвоенными кавычками
    /// внутри (иначе Excel при вставке разорвал бы ячейку или снял бы кавычку), иначе как есть.
    /// </summary>
    /// <returns>Текст ячейки.</returns>
    private static string Quoted(string text) =>
        NeedsQuotes(text) ? $"\"{text.Replace("\"", "\"\"")}\"" : text;

    /// <summary>Нужны ли ячейке кавычки при вставке в Excel: есть табуляция, CR или LF, или текст начинается с кавычки.</summary>
    /// <returns><c>true</c>, если ячейку нужно взять в кавычки.</returns>
    private static bool NeedsQuotes(string text) => text.IndexOfAny(Breaks) >= 0 || text.StartsWith('"');
}
