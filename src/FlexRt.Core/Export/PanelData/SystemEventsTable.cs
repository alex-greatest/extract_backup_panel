using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Таблица листа «Системные события»: номер события и его текст на каждом языке панели, как System events в TIA.</summary>
public static class SystemEventsTable
{
    /// <summary>
    /// Построить таблицу. Колонки: <c>ID</c> и по колонке на язык в порядке STRINGSTORE
    /// (<c>0x409 — English (United States)</c>). Строки — по событию, по возрастанию ID; ID —
    /// число, текст — строка языка с номером <see cref="SystemEvent.TextIdx"/>, нет такой строки —
    /// пустая ячейка.
    /// </summary>
    /// <returns>Заголовки и строки событий.</returns>
    public static ReportTable Build(FwxDocument doc)
    {
        var languages = doc.Strings.Select(s => s.Lcid).Distinct().ToList();
        var texts = doc.Strings.ToLookup(s => (s.Lcid, (long)s.Idx), s => s.Str);
        var columns = languages.Select(LanguageText.Format).Prepend("ID").ToList();
        var rows = doc.SystemEvents.OrderBy(e => e.Id).Select(Row).ToList();
        return new ReportTable(columns, rows);

        // строка одного события: ID и текст на каждом языке; нет строки языка — пусто
        ReportCell[] Row(SystemEvent systemEvent) => languages
            .Select(lcid => (ReportCell)(texts[(lcid, systemEvent.TextIdx)].FirstOrDefault() ?? ""))
            .Prepend(systemEvent.Id)
            .ToArray();
    }
}
