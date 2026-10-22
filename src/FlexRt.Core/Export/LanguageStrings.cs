using System.Runtime.CompilerServices;
using FlexRt.Model.Panel;

namespace FlexRt.Export;

/// <summary>
/// Строки языков панели (STRINGSTORE) по языку и номеру строки: колонки текстов по языкам и ячейки строки на каждом языке.
/// Общие для листов, где текст — строка языков с номером из записи (алармы, системные события, подсказки экранов).
/// </summary>
public sealed class LanguageStrings
{
    /// <summary>Строки уже собранных документов: на документ одни, листы панели их не собирают заново.</summary>
    private static readonly ConditionalWeakTable<FwxDocument, LanguageStrings> Built = [];

    /// <summary>Тексты строк по языку и номеру строки.</summary>
    private readonly ILookup<(int Lcid, long Idx), string> _texts;

    /// <summary>Собрать языки и строки документа.</summary>
    private LanguageStrings(FwxDocument doc)
    {
        Languages = LanguageText.Languages(doc);
        _texts = doc.Strings.ToLookup(s => (s.Lcid, (long)s.Idx), s => s.Str);
    }

    /// <summary>
    /// Строки языков документа: собираются при первом вызове и запоминаются до сборки документа сборщиком мусора. Документ
    /// после чтения не меняется, поэтому листы панели (алармы, системные события, подсказки экранов) берут одни строки.
    /// </summary>
    /// <returns>Строки языков документа.</returns>
    public static LanguageStrings Of(FwxDocument doc) => Built.GetValue(doc, static d => new LanguageStrings(d));

    /// <summary>Языки панели в порядке STRINGSTORE, без повторов (<see cref="LanguageText.Languages"/>).</summary>
    public IReadOnlyList<int> Languages { get; }

    /// <summary>
    /// Колонки текста: по колонке на язык (<c>0x409 — English (United States)</c>), перед языком — <paramref name="prefix"/>,
    /// если задан (<see cref="LanguageText.Columns"/>).
    /// </summary>
    /// <returns>По колонке на язык.</returns>
    public IEnumerable<ReportColumn> Columns(string? prefix = null) => LanguageText.Columns(Languages, prefix);

    /// <summary>Строка языка <paramref name="lcid"/> с номером <paramref name="idx"/>; строк с номером несколько — первая.</summary>
    /// <returns>Текст строки как есть; нет такой строки — пусто.</returns>
    public string Text(int lcid, long idx) => _texts[(lcid, idx)].FirstOrDefault() ?? "";

    /// <summary>Ячейки строки с номером <paramref name="idx"/> на каждом языке как есть; нет строки на языке — пустая ячейка.</summary>
    /// <returns>По ячейке на язык.</returns>
    public IEnumerable<ReportCell> Cells(long idx) => Languages.Select(lcid => (ReportCell)Text(lcid, idx));
}
