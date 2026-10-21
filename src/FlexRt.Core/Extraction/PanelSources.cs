using FlexRt.Export.PanelData;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Summary;
using FlexRt.Parsing.Matching;

namespace FlexRt.Extraction;

/// <summary>Данные для листов <c>panel_data.xlsx</c> (<see cref="PanelDataSource"/>) по прочитанной панели и данным ПЛК.</summary>
public static class PanelSources
{
    /// <summary>
    /// Значение строки «Данные ПЛК» в «Сводке», когда данные ПЛК не выбраны или не поддерживаются для панели с
    /// несколькими соединениями (решения пользователя).
    /// </summary>
    public const string NoPlc = "не задан";

    /// <summary>
    /// Собрать данные листов. Данные ПЛК прочитаны и поддерживаются (<paramref name="plc"/> не <c>null</c> и не
    /// <see cref="PlcData.IsUnsupported"/>) — проект ПЛК, сопоставление и строка «Данные ПЛК» из
    /// <see cref="PlcData.Source"/>. Иначе теги сопоставляются без проекта ПЛК (<see cref="TagMatcher.Match"/> с
    /// <c>null</c>): колонки ПЛК — «неизвестно», «Данные ПЛК» — <see cref="NoPlc"/>. Сопоставление нужно «Тегам» и
    /// «Дискретным алармам»: <see cref="Lazy{T}"/> считает его один раз и повторяет его ошибку обоим.
    /// </summary>
    /// <param name="doc">Прочитанная панель.</param>
    /// <param name="path">Путь к <c>pdata.fwc</c> для «Сводки».</param>
    /// <param name="plc">Данные ПЛК; <c>null</c> — не выбраны.</param>
    /// <returns>Данные листов.</returns>
    public static PanelDataSource From(FwxDocument doc, string path, PlcData? plc)
    {
        var used = plc is { IsUnsupported: false } ? plc : null;
        var matches = new Lazy<IReadOnlyList<PlcMatch?>>(() => used?.Matches ?? TagMatcher.Match(doc, null));
        return new PanelDataSource(doc, used?.Project, new RunSummary(path, used?.Source ?? NoPlc), matches);
    }
}
