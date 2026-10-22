using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>
/// Тексты алармов на языках панели: колонки текстов и ячейки Display name своего класса; общие для листов
/// «Дискретные алармы», «Аналоговые алармы» и «Классы алармов».
/// </summary>
public sealed class AlarmLanguageTexts
{
    /// <summary>Строки языков панели.</summary>
    private readonly LanguageStrings _strings;

    /// <summary>Имена HMI-тегов полей в тексте.</summary>
    private readonly AlarmFieldNames _fieldNames;

    /// <summary>Собрать языки и строки документа и индекс имён тегов полей.</summary>
    public AlarmLanguageTexts(FwxDocument doc)
    {
        _strings = LanguageStrings.Of(doc);
        _fieldNames = new AlarmFieldNames(doc);
    }

    /// <summary>
    /// Колонки текста: по колонке на язык (<c>0x409 — English (United States)</c>), перед языком — <paramref name="prefix"/>,
    /// если задан (<see cref="LanguageText.Columns"/>).
    /// </summary>
    /// <returns>По колонке на язык.</returns>
    public IEnumerable<ReportColumn> Columns(string? prefix = null) => _strings.Columns(prefix);

    /// <summary>
    /// Ячейки текста аларма по языкам: строка с номером <paramref name="textIdx"/> с подписями
    /// полей (<see cref="AlarmText.Format"/>); нет строки на языке — пустая ячейка.
    /// </summary>
    /// <returns>По ячейке на язык.</returns>
    public IEnumerable<ReportCell> Cells(long textIdx, IReadOnlyList<AlarmField> fields)
    {
        var labels = AlarmText.FieldLabels(fields, _fieldNames);
        return _strings.Languages.Select(lcid => _strings.Text(lcid, textIdx))
            .Select(text => (ReportCell)(text.Length > 0 ? AlarmText.Format(text, labels) : ""));
    }

    /// <summary>Ячейки строки с номером <paramref name="idx"/> на каждом языке как есть, без полей; нет строки — пустая ячейка.</summary>
    /// <returns>По ячейке на язык.</returns>
    public IEnumerable<ReportCell> PlainCells(long idx) => _strings.Cells(idx);

    /// <summary>
    /// Ячейка строки с номером <paramref name="idx"/> (Display name своего класса в колонке Alarm class): текст — на первом
    /// языке панели (его пишет Excel), <see cref="ReportCell.LanguageTexts"/> — на каждом языке (их показывает окно).
    /// </summary>
    /// <returns>Ячейка; языков или строки нет — пустой текст.</returns>
    public ReportCell LanguageCell(long idx)
    {
        var languages = _strings.Languages;
        return (ReportCell)(languages.Count == 0 ? "" : _strings.Text(languages[0], idx)) with
        {
            LanguageTexts = languages.ToDictionary(lcid => lcid, lcid => _strings.Text(lcid, idx))
        };
    }
}
