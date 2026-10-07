namespace FlexRt.Model.Plc;

/// <summary>Тег ПЛК из проекта TIA (PEData.plf).</summary>
/// <param name="Name">Имя тега ПЛК.</param>
/// <param name="PlcId">ID объекта ПЛК, которому принадлежит тег.</param>
/// <param name="DataType">Тип данных, как в TIA (<c>Bool</c>, <c>LTime_Of_Day</c>); <c>null</c>, если не найден.</param>
/// <param name="Address">Логический адрес (<c>%I0.0</c>, <c>%QD50</c>, <c>%T0</c>).</param>
/// <param name="Area">Область памяти; <c>null</c>, если код области неизвестен.</param>
/// <param name="SymbolId">ID символа.</param>
/// <param name="Comment">Комментарий тега на первом языке; пустая строка, если его нет.</param>
public sealed record PlcTag(string Name, long PlcId, string? DataType, string Address, PlcArea? Area, long SymbolId, string Comment);
