namespace FlexRt.Model.Panel;

/// <summary>Таблица из Table Of Contents.</summary>
public sealed class TocEntry
{
    /// <summary>Число элементов таблицы.</summary>
    public int Entries { get; init; }
    /// <summary>Наблюдение: у всех таблиц 0x64 (версия 1.00?).</summary>
    public int Version { get; init; }
    /// <summary>Наблюдение: у всех таблиц 0x01.</summary>
    public int Parent { get; init; }
    /// <summary>Идентификатор таблицы; совпадает с индексом в TOC.</summary>
    public int Id { get; init; }
    /// <summary>Имя таблицы (<c>VAR</c>, <c>STRINGSTORE</c>, …).</summary>
    public string Name { get; init; } = "";
    /// <summary>Смещение таблицы от начала файла.</summary>
    public long Offset { get; init; }
    /// <summary>Длина всей таблицы в байтах: до следующей таблицы по физическому порядку.</summary>
    public long Size { get; set; }
}
