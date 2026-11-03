using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Тег I/O field (Process → Tag) по ссылкам элемента поля: прямая ссылка на VAR (сверено с TIA на шаговых копиях
/// <c>data/real/steps-iofield/</c>: внутренние теги и тег ПЛК) или первая ссылка на ADAPTVALUECHANG, первая ссылка
/// которого ведёт на VAR или на элемент VARLIST с именем тега (сверено с TIA через Openness: <c>IO Field_2</c>
/// A603A0097 → <c>Step_Number</c>).
/// </summary>
internal static class IoFieldTagReader
{
    /// <summary>Таблица тегов.</summary>
    private const string VarTable = "VAR";

    /// <summary>Посредник: u32 1, u32 R, R ссылок — первая на тег.</summary>
    private const string ValueChangeTable = "ADAPTVALUECHANG";

    /// <summary>Таблица с именованными элементами-тегами (начало — как у VAR: u16 3, u16 длина имени, имя).</summary>
    private const string VarListTable = "VARLIST";

    /// <summary>
    /// Тег поля: прямая ссылка на VAR, иначе — через ADAPTVALUECHANG первой ссылки. Тип — код типа записи VAR
    /// (<see cref="HmiTag.TypeCode"/>; номер записи у тега — с единицы); у тега из VARLIST типа нет.
    /// </summary>
    /// <param name="doc">Документ панели (теги VAR уже прочитаны).</param>
    /// <param name="item">Элемент поля.</param>
    /// <returns>
    /// Имя и тип тега; тег не найден, а ссылки есть — <c>Via</c>: таблица и номер элемента первой ссылки с единицы
    /// (<c>RECVAR 4</c>; таблицы нет в TOC — <c>?</c> вместо имени); ссылок нет — всё <c>null</c>.
    /// </returns>
    /// <exception cref="FwxFormatException">Элемент посредника или тега не сходится.</exception>
    public static (string? Name, int? Type, string? Via) Read(FwxDocument doc, TableItem item)
    {
        var links = NamedElement.TryReferences(doc.Binary, item) ?? [];
        if (links.Count == 0)
        {
            return (null, null, null);
        }
        var varId = doc.FindTable(VarTable)?.Id;
        var direct = links.ToList().FindIndex(r => r.Table == varId);
        var link = direct >= 0 ? links[direct] : Through(doc, links[0]);
        if (link is { } tag && Tag(doc, tag) is { } found)
        {
            return (found.Name, found.Type, null);
        }
        return (null, null, $"{doc.FindTable(links[0].Table)?.Name ?? "?"} {links[0].Index + 1}");
    }

    /// <summary>Первая ссылка элемента ADAPTVALUECHANG, на который ведёт <paramref name="link"/>.</summary>
    /// <returns>
    /// Ссылка; таблица не ADAPTVALUECHANG, ссылка за последний элемент или у элемента нет ссылок — <c>null</c> (тег
    /// тогда не найден, остальные свойства поля читаются).
    /// </returns>
    /// <exception cref="FwxFormatException">Элемент посредника вне таблицы или не сходится.</exception>
    private static VarReference? Through(FwxDocument doc, VarReference link)
    {
        if (doc.FindTable(link.Table) is not { Name: ValueChangeTable } table || link.Index >= table.Entries)
        {
            return null;
        }
        var (_, links) = ReferenceHeader.Read(doc.Binary, ElementLink.Item(doc, table, link.Index), table.Name);
        return links.Count > 0 ? links[0] : null;
    }

    /// <summary>Имя и тип тега по ссылке на VAR или VARLIST.</summary>
    /// <returns>
    /// Имя и тип (у VARLIST — <c>null</c>); таблица другая, ссылка за последний элемент или имя не читается — <c>null</c>
    /// (тег не найден, остальные свойства поля читаются).
    /// </returns>
    /// <exception cref="FwxFormatException">Элемент вне таблицы.</exception>
    private static (string Name, int? Type)? Tag(FwxDocument doc, VarReference link)
    {
        if (doc.FindTable(link.Table) is not { Name: VarTable or VarListTable } table || link.Index >= table.Entries)
        {
            return null;
        }
        if (NamedElement.TryName(doc.Binary, ElementLink.Item(doc, table, link.Index)) is not { Length: > 0 } name)
        {
            return null;
        }
        return (name, table.Name == VarTable ? doc.Tags.FirstOrDefault(t => t.Index == link.Index + 1)?.TypeCode : null);
    }
}
