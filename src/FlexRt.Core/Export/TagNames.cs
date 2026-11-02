using FlexRt.Model.Panel;

namespace FlexRt.Export;

/// <summary>
/// Имя тега по ссылке на запись VAR (u16 Id таблицы, u16 номер записи с нуля) — для колонок Visibility листа «Экраны»
/// и параметров функций вкладки Events окна.
/// </summary>
public sealed class TagNames
{
    /// <summary>Id таблицы VAR из TOC; таблицы нет — <c>null</c>.</summary>
    private readonly int? _varTable;

    /// <summary>Имя тега по номеру записи VAR с единицы (<see cref="HmiTag.Index"/>).</summary>
    private readonly Dictionary<int, string> _names;

    /// <summary>Собрать Id таблицы VAR и имена тегов документа один раз.</summary>
    public TagNames(FwxDocument doc)
    {
        _varTable = doc.FindTable("VAR")?.Id;
        _names = doc.Tags.DistinctBy(t => t.Index).ToDictionary(t => t.Index, t => t.Name);
    }

    /// <summary>Имя тега по ссылке: номер записи с нуля (<see cref="HmiTag.Index"/> — с единицы).</summary>
    /// <returns>Имя; ссылки нет, она не на VAR или запись не прочитана — <c>null</c>.</returns>
    public string? Of(VarReference? tag) =>
        tag is { } t && t.Table == _varTable ? _names.GetValueOrDefault(t.Index + 1) : null;
}
