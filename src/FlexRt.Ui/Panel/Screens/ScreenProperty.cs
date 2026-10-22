using FlexRt.Export;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Свойство экрана для панели Properties — ячейка строки листа «Экраны» под заголовком своей колонки.</summary>
/// <param name="Name">Заголовок колонки листа — имя свойства.</param>
/// <param name="Value">Значение ячейки.</param>
/// <param name="Language">
/// Язык (LCID) колонки текста на одном языке панели — из <see cref="ReportTable.ColumnLanguages"/> ядра, а не из
/// заголовка (колонки Tooltip); <c>null</c> — свойство не языковое.
/// </param>
/// <param name="Group">
/// Группа колонок текста на языках — из <see cref="ReportTable.ColumnGroups"/> ядра (<c>Tooltip</c>); <c>null</c> —
/// колонка не в группе.
/// </param>
public sealed record ScreenProperty(string Name, ReportCell Value, int? Language, string? Group);
