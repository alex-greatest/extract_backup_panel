using FlexRt.Model.Panel;

namespace FlexRt.Model.Summary;

/// <summary>Сведения о запуске для листа «Сводка»: что прочитано и откуда.</summary>
/// <param name="PanelPath">Путь к прочитанному <c>pdata.fwc</c>.</param>
/// <param name="PanelFiles">Модель панели и версия Runtime из файлов рядом с <c>pdata.fwc</c>.</param>
/// <param name="PlcSource">Откуда данные ПЛК: <c>карта ПЛК: путь</c>, <c>PEData.plf: путь</c>, <c>не найден: путь</c>, <c>не прочитан: путь</c>.</param>
public sealed record RunSummary(string PanelPath, PanelFiles PanelFiles, string PlcSource);
