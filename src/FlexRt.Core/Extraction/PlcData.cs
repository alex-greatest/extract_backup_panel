using FlexRt.Model.Matching;
using FlexRt.Model.Plc;

namespace FlexRt.Extraction;

/// <summary>Итог чтения данных ПЛК для прочитанной панели (<see cref="PlcDataReader.Read"/>).</summary>
/// <param name="IsUnsupported">Сочетание не поддерживается: у панели несколько соединений, а данные ПЛК заданы; тогда остальное пусто.</param>
/// <param name="Project">Проект ПЛК; <c>null</c>, если не найден, не прочитан или сочетание не поддерживается.</param>
/// <param name="Source">Откуда данные ПЛК для листа «Сводка»: <c>карта ПЛК: путь</c>, <c>PEData.plf: путь</c>, <c>не найден: путь</c>, <c>не прочитан: путь</c>; <c>null</c>, если сочетание не поддерживается.</param>
/// <param name="Matches">Сопоставление тегов панели с ПЛК по порядку тегов (<c>null</c> — внутренний тег); пусто, если сочетание не поддерживается.</param>
/// <param name="Errors">Сообщения о проблемах проекта ПЛК (вкладка «Ошибки» окна).</param>
public sealed record PlcData(bool IsUnsupported, PlcProject? Project, string? Source, IReadOnlyList<PlcMatch?> Matches, IReadOnlyList<string> Errors);
