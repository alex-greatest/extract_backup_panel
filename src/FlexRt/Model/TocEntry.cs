namespace FlexRt.Model;

/// <summary>Таблица из Table Of Contents.</summary>
public sealed class TocEntry
{
    public int Entries { get; init; }
    public int Version { get; init; }
    public int Parent { get; init; }
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public long Offset { get; init; }
    /// <summary>Длина всей таблицы в байтах: до следующей таблицы по физическому порядку.</summary>
    public long Size { get; set; }
}
