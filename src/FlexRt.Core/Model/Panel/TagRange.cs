namespace FlexRt.Model.Panel;

/// <summary>Range тега из LIMIT; null у границы — она выключена в прочитанной записи.</summary>
/// <param name="IsRead">Все границы прочитаны; false — запись не разобрана.</param>
/// <param name="Lower2">Нижняя граница 2.</param>
/// <param name="Upper2">Верхняя граница 2.</param>
/// <param name="Lower1">Нижняя граница 1.</param>
/// <param name="Upper1">Верхняя граница 1.</param>
public sealed record TagRange(bool IsRead, TagRangeLimit? Lower2 = null, TagRangeLimit? Upper2 = null,
    TagRangeLimit? Lower1 = null, TagRangeLimit? Upper1 = null);
