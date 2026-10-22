using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Screens;

/// <summary>
/// Подсказки (Tooltip) экранов на языках панели: пары INFO_HANDLER с Id таблицы SCREEN, текст — строки языков с номером
/// из пары, как есть.
/// </summary>
public sealed class ScreenTooltips
{
    /// <summary>Начало заголовка колонки подсказки, за ним язык (<c>Tooltip 0x409 — English (United States)</c>).</summary>
    private const string ColumnPrefix = "Tooltip";

    /// <summary>Номер строки языков подсказки по номеру записи SCREEN (с нуля).</summary>
    private readonly Dictionary<int, int> _textIndexes;

    /// <summary>Строки языков панели.</summary>
    private readonly LanguageStrings _strings;

    /// <summary>Собрать пары подсказок экранов (Id таблицы SCREEN из TOC) и строки документа; таблицы SCREEN нет — подсказок нет.</summary>
    public ScreenTooltips(FwxDocument doc)
    {
        var screenTable = doc.FindTable("SCREEN")?.Id;
        _textIndexes = doc.InfoTexts
            .Where(t => t.Owner.Table == screenTable)
            .DistinctBy(t => t.Owner.Index)
            .ToDictionary(t => t.Owner.Index, t => t.TextIndex);
        _strings = LanguageStrings.Of(doc);
    }

    /// <summary>Колонки подсказки: по колонке на язык (<see cref="LanguageText.Columns"/> с началом <c>Tooltip</c>).</summary>
    /// <returns>По колонке на язык.</returns>
    public IEnumerable<ReportColumn> Columns() => _strings.Columns(ColumnPrefix);

    /// <summary>
    /// Ячейки подсказки записи SCREEN <paramref name="screenIndex"/> на каждом языке: текст как есть (перенос — CRLF);
    /// пары нет или нет строки на языке — пустая ячейка. Пар у записи несколько — берётся первая.
    /// </summary>
    /// <returns>По ячейке на язык.</returns>
    public IEnumerable<ReportCell> Cells(int screenIndex) =>
        _textIndexes.TryGetValue(screenIndex, out var idx)
            ? _strings.Cells(idx)
            : _strings.Languages.Select(_ => (ReportCell)"");
}
