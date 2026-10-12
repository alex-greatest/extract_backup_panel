using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Model.Matching;

/// <summary>Результат поиска тега панели в проекте ПЛК.</summary>
/// <param name="Lookup">Чем закончился поиск.</param>
/// <param name="PlcName">Имя выбранного ПЛК; <c>null</c>, если ПЛК не выбран или имя не найдено.</param>
/// <param name="Tag">
/// Найденный тег ПЛК; <c>null</c>, если не найден. У <see cref="PlcLookup.AddressOnly"/> с
/// адресом в DB — сам DB (имя DB в <see cref="PlcTag.Name"/>), если он есть в ПЛК.
/// </param>
/// <param name="Link">
/// Связь тега панели с ПЛК (элемент DATALINK_READWR или DATALINK); <c>null</c>, если запись
/// VAR ссылается на незнакомую таблицу или за пределы таблицы.
/// </param>
public sealed record PlcMatch(PlcLookup Lookup, string? PlcName, PlcTag? Tag, PlcLink? Link = null);
