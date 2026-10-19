using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;

namespace FlexRt.Export.PanelData;

/// <summary>Данные, из которых строятся листы <c>panel_data.xlsx</c> (<see cref="PanelDataBook"/>).</summary>
/// <param name="Doc">Прочитанная панель.</param>
/// <param name="Project">Проект ПЛК для «Сводки»; нет — <c>null</c>.</param>
/// <param name="Summary">Сведения о запуске для «Сводки».</param>
/// <param name="Matches">
/// Сопоставление тегов с ПЛК — его берут «Теги» и «Дискретные алармы». Считается при построении первого из этих листов
/// один раз, его ошибка повторяется обоим (<see cref="Lazy{T}"/> запоминает исключение): сбой сопоставления — сбой
/// только их.
/// </param>
public sealed record PanelDataSource(FwxDocument Doc, PlcProject? Project, RunSummary Summary, Lazy<IReadOnlyList<PlcMatch?>> Matches);
