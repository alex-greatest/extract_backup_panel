using FlexRt.Binary;

namespace FlexRt.Model;

/// <summary>Разобранный FWX: заголовок, TOC, строки STRINGSTORE и предупреждения.</summary>
public sealed class FwxDocument
{
    public required FwxBinary Binary { get; init; }
    public required FwxHeader Header { get; init; }
    public List<TocEntry> Toc { get; } = [];
    /// <summary>Строки всех языков из STRINGSTORE в порядке каталога.</summary>
    public List<LangString> Strings { get; } = [];
    /// <summary>Таблицы, которые не удалось разобрать (остальное при этом читается).</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>Таблица по имени из TOC (<c>VAR</c>, <c>STRINGSTORE</c>, …) или <c>null</c>.</summary>
    public TocEntry? FindTable(string name) => Toc.FirstOrDefault(t => t.Name == name);
    /// <summary>Есть ли в TOC таблица с таким именем.</summary>
    public bool HasTable(string name) => FindTable(name) is not null;
}
