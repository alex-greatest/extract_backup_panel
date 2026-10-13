using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>
/// Тексты алармов на языках панели: колонки текстов и строки первого языка; общие для листов
/// «Дискретные алармы», «Аналоговые алармы» и «Классы алармов».
/// </summary>
public sealed class AlarmLanguageTexts
{
    /// <summary>Языки панели в порядке STRINGSTORE.</summary>
    private readonly List<int> _languages;

    /// <summary>Тексты строк по языку и номеру строки.</summary>
    private readonly ILookup<(int Lcid, long Idx), string> _texts;

    /// <summary>Имена HMI-тегов полей в тексте.</summary>
    private readonly AlarmFieldNames _fieldNames;

    /// <summary>Собрать языки и строки документа и индекс имён тегов полей.</summary>
    public AlarmLanguageTexts(FwxDocument doc)
    {
        _languages = [.. doc.Strings.Select(s => s.Lcid).Distinct()];
        _texts = doc.Strings.ToLookup(s => (s.Lcid, (long)s.Idx), s => s.Str);
        _fieldNames = new AlarmFieldNames(doc);
    }

    /// <summary>Заголовки колонок текста: по колонке на язык (<c>0x409 — English (United States)</c>).</summary>
    public IEnumerable<string> Columns => _languages.Select(LanguageText.Format);

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

    /// <summary>Строка с номером <paramref name="idx"/> на первом языке панели (Display name своего класса).</summary>
    /// <returns>Текст строки; языков или строки нет — пусто.</returns>
    public string FirstLanguageText(long idx) => _languages.Count == 0 ? "" : Text(_languages[0], idx);

    /// <summary>Строка языка <paramref name="lcid"/> с номером <paramref name="idx"/>.</summary>
    /// <returns>Текст строки; нет такой строки — пусто.</returns>
    private string Text(int lcid, long idx) => _texts[(lcid, idx)].FirstOrDefault() ?? "";
}
