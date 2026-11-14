namespace FlexRt.Model.Panel.Screens;

/// <summary>Условие Visibility или Control enable: диапазон либо один бит.</summary>
/// <param name="Kind">Назначение условия.</param>
/// <param name="Tag">Имя тега условия.</param>
/// <param name="Bit">Номер бита; null — Range.</param>
/// <param name="From">Начало диапазона.</param>
/// <param name="To">Конец диапазона.</param>
/// <param name="Active">В диапазоне или при установленном бите объект виден/доступен; null — не прочитано.</param>
public sealed record FieldCondition(FieldConditionKind Kind, string? Tag, int? Bit, double? From, double? To, bool? Active);
