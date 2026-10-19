using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>
/// Описание листов <c>panel_data.xlsx</c> — одно на консоль и окно: какие листы есть, из каких таблиц и в каком порядке.
/// Лист «Ошибки» сюда не входит: он только у консоли (<see cref="PanelDataExporter"/>).
/// </summary>
public static class PanelDataBook
{
    /// <summary>
    /// Листы в порядке книги: «Сводка» и «Теги» — всегда (и без тегов); «Системные события», «Дискретные алармы»,
    /// «Аналоговые алармы», «Классы алармов», «Группы алармов», «Экраны» — если в их таблице есть строки.
    /// </summary>
    public static IReadOnlyList<PanelDataSheet> Sheets { get; } =
    [
        new(SummarySheet.Name, true, static _ => true, static s => SummaryRows.Table(s.Doc, s.Project, s.Summary), static _ => []),
        new(TagsSheet.Name, false, static _ => true, static s => TagsTable.Build(s.Doc, s.Matches.Value),
            static s => TagStatistics.Rows(s.Doc, s.Matches.Value)),
        Table(SystemEventsSheet.Name, static doc => doc.SystemEvents.Count > 0, static s => SystemEventsTable.Build(s.Doc)),
        Table(DiscreteAlarmsSheet.Name, static doc => doc.DiscreteAlarms.Count > 0, static s => DiscreteAlarmsTable.Build(s.Doc, s.Matches.Value)),
        Table(AnalogAlarmsSheet.Name, static doc => doc.AnalogAlarms.Count > 0, static s => AnalogAlarmsTable.Build(s.Doc)),
        Table(AlarmClassesSheet.Name, static doc => AlarmClassesTable.Classes(doc).Count > 0, static s => AlarmClassesTable.Build(s.Doc)),
        Table(AlarmGroupsSheet.Name, static doc => AlarmGroupsTable.Groups(doc).Count > 0, static s => AlarmGroupsTable.Build(s.Doc)),
        // строки листа — экраны без служебных записей (ScreensTable.Screens)
        Table(ScreensSheet.Name, static doc => doc.Screens.Any(s => !s.IsService), static s => ScreensTable.Build(s.Doc))
    ];

    /// <summary>
    /// Листы, которые есть в книге, по порядку <see cref="Sheets"/>; каждый строится, когда до него доходит перебор.
    /// Сбой любого листа прерывает перебор.
    /// </summary>
    /// <returns>Построенные листы.</returns>
    public static IEnumerable<ReportSheet> Build(PanelDataSource source) =>
        Sheets.Where(s => s.Exists(source.Doc)).Select(s => Build(s, source));

    /// <summary>
    /// Листы по порядку <see cref="Sheets"/>, каждый отдельно: сбой условия наличия или построителя одного листа даёт
    /// <see cref="BuiltSheet"/> с ошибкой и пустым листом, остальные строятся. Листа нет в книге — его нет и в итоге.
    /// </summary>
    /// <returns>Итоги листов, которые есть в книге или не построены.</returns>
    public static IReadOnlyList<BuiltSheet> TryBuild(PanelDataSource source) =>
        [.. Sheets.Select(s => TryBuild(s, source)).OfType<BuiltSheet>()];

    /// <summary>Построить один лист, перехватив его сбой.</summary>
    /// <returns>Лист или ошибка с пустым листом той же раскладки; листа нет в книге — <c>null</c>.</returns>
    private static BuiltSheet? TryBuild(PanelDataSheet sheet, PanelDataSource source)
    {
        try
        {
            return sheet.Exists(source.Doc) ? new BuiltSheet(Build(sheet, source), null) : null;
        }
        catch (Exception e)
        {
            return new BuiltSheet(new ReportSheet(sheet.Name, new ReportTable([], []), [], sheet.IsSummary), e);
        }
    }

    /// <summary>Лист по описанию: таблица, затем статистика.</summary>
    /// <returns>Лист.</returns>
    private static ReportSheet Build(PanelDataSheet sheet, PanelDataSource source) =>
        new(sheet.Name, sheet.Table(source), sheet.Statistics(source), sheet.IsSummary);

    /// <summary>Лист-таблица с первой строки: есть, если <paramref name="exists"/> для панели.</summary>
    /// <returns>Описание листа.</returns>
    private static PanelDataSheet Table(string name, Func<FwxDocument, bool> exists, Func<PanelDataSource, ReportTable> table) =>
        new(name, false, exists, table, static _ => []);
}
