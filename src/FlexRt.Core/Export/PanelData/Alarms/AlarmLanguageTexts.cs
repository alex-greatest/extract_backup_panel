using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>
/// Тексты алармов на языках панели: колонки текстов и ячейки Display name своего класса; общие для листов
/// «Дискретные алармы», «Аналоговые алармы» и «Классы алармов».
/// </summary>
public sealed class AlarmLanguageTexts
{
    /// <summary>Языки панели в порядке STRINGSTORE.</summary>
    private readonly IReadOnlyList<int> _languages;

    /// <summary>Тексты строк по языку и номеру строки.</summary>
    private readonly ILookup<(int Lcid, long Idx), string> _texts;

    /// <summary>Имена HMI-тегов полей в тексте.</summary>
    private readonly AlarmFieldNames _fieldNames;

    /// <summary>Собрать языки и строки документа и индекс имён тегов полей.</summary>
    public AlarmLanguageTexts(FwxDocument doc)
    {
        _languages = LanguageText.Languages(doc);
        _texts = doc.Strings.ToLookup(s => (s.Lcid, (long)s.Idx), s => s.Str);
        _fieldNames = new AlarmFieldNames(doc);
    }

    /// <summary>
    /// Колонки текста: по колонке на язык (<c>0x409 — English (United States)</c>), перед языком — <paramref name="prefix"/>,
    /// если задан (<see cref="LanguageText.Columns"/>).
    /// </summary>
    /// <returns>По колонке на язык.</returns>
    public IEnumerable<ReportColumn> Columns(string? prefix = null) => LanguageText.Columns(_languages, prefix);

    /// <summary>
    /// Ячейки текста аларма по языкам: строка с номером <paramref name="textIdx"/> с подписями
    /// полей (<see cref="AlarmText.Format"/>); нет строки на языке — пустая ячейка.
    /// </summary>
    /// <returns>По ячейке на язык.</returns>
    public IEnumerable<ReportCell> Cells(long textIdx, IReadOnlyList<AlarmField> fields)
    {
        var labels = AlarmText.FieldLabels(fields, _fieldNames);
        return _languages.Select(lcid => Text(lcid, textIdx))
            .Select(text => (ReportCell)(text.Length > 0 ? AlarmText.Format(text, labels) : ""));
    }

    /// <summary>Ячейки строки с номером <paramref name="idx"/> на каждом языке как есть, без полей; нет строки — пустая ячейка.</summary>
    /// <returns>По ячейке на язык.</returns>
    public IEnumerable<ReportCell> PlainCells(long idx) => _languages.Select(lcid => (ReportCell)Text(lcid, idx));

    /// <summary>
    /// Ячейка строки с номером <paramref name="idx"/> (Display name своего класса в колонке Alarm class): текст — на первом
    /// языке панели (его пишет Excel), <see cref="ReportCell.LanguageTexts"/> — на каждом языке (их показывает окно).
    /// </summary>
    /// <returns>Ячейка; языков или строки нет — пустой текст.</returns>
    public ReportCell LanguageCell(long idx) =>
        ((ReportCell)(_languages.Count == 0 ? "" : Text(_languages[0], idx))) with
        {
            LanguageTexts = _languages.ToDictionary(lcid => lcid, lcid => Text(lcid, idx))
        };

    /// <summary>Строка языка <paramref name="lcid"/> с номером <paramref name="idx"/>.</summary>
    /// <returns>Текст строки; нет такой строки — пусто.</returns>
    private string Text(int lcid, long idx) => _texts[(lcid, idx)].FirstOrDefault() ?? "";
}
