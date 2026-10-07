namespace FlexRt.Model.Plc;

/// <summary>Связь объекта PEData.plf с другим объектом: запись <c>[тип][класс цели][ID цели][0]</c>.</summary>
/// <param name="Type">Тип связи.</param>
/// <param name="Class">Класс объекта-цели.</param>
/// <param name="Id">ID объекта-цели.</param>
internal readonly record struct PlfRelation(long Type, long Class, long Id);
