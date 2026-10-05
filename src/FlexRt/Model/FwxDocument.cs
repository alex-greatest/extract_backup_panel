using FlexRt.Binary;

namespace FlexRt.Model;

/// <summary>Разобранный FWX: заголовок, TOC, строки STRINGSTORE, теги VAR и предупреждения.</summary>
public sealed class FwxDocument
{
    /// <summary>Байты файла целиком.</summary>
    public required FwxBinary Binary { get; init; }
    /// <summary>Заголовок файла.</summary>
    public required FwxHeader Header { get; init; }
    /// <summary>Непустые записи Table Of Contents в порядке TOC.</summary>
    public List<TocEntry> Toc { get; } = [];
    /// <summary>Строки всех языков из STRINGSTORE в порядке каталога.</summary>
    public List<LangString> Strings { get; } = [];
    /// <summary>Теги HMI из VAR в порядке каталога.</summary>
    public List<HmiTag> Tags { get; } = [];
    /// <summary>Таблицы, которые не удалось разобрать (остальное при этом читается).</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>Таблица по имени из TOC (<c>VAR</c>, <c>STRINGSTORE</c>, …) или <c>null</c>.</summary>
    public TocEntry? FindTable(string name) => Toc.FirstOrDefault(t => t.Name == name);
}
