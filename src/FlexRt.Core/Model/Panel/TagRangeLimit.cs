namespace FlexRt.Model.Panel;

/// <summary>Числовая или ссылочная граница Range; значение тега во время исполнения не хранится в FWC.</summary>
/// <param name="Value">Постоянное числовое значение.</param>
/// <param name="Tag">Имя тега ссылочной границы; у постоянной — null.</param>
public sealed record TagRangeLimit(double? Value = null, string? Tag = null);
