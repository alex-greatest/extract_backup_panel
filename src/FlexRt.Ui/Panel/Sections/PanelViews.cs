using FlexRt.Export;
using FlexRt.Export.PanelData;
using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Extraction;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Summary;
using FlexRt.Parsing.Matching;

namespace FlexRt.Ui.Panel.Sections;

/// <summary>
/// Разделы прочитанной панели для окна. Таблицы строит ядро теми же методами, что пишут листы Excel, поэтому окно
/// показывает то же, что консоль выгрузила бы с теми же данными ПЛК. Разделы строятся независимо: сбой одного не
/// мешает остальным и экранам (решение пользователя).
/// </summary>
public static class PanelViews
{
    /// <summary>
    /// Значение строки «Данные ПЛК» в «Сводке», когда данные ПЛК в окне не выбраны или не поддерживаются для панели с
    /// несколькими соединениями (решения пользователя).
    /// </summary>
    private const string NoPlc = "не задан";

    /// <summary>Разделитель частей пути в заголовке таблицы, как у экранов.</summary>
    private const string Separator = "  ▸  ";

    /// <summary>Папка алармов в дереве, как в TIA.</summary>
    private const string AlarmsFolder = "HMI alarms";

    /// <summary>Папка текстов в дереве.</summary>
    private const string TextsFolder = "Тексты";

    /// <summary>Колонки таблицы «Сводки»; в окне не показываются — у листа заголовков нет.</summary>
    private static readonly string[] SummaryColumns = ["Параметр", "Значение"];

    /// <summary>Колонки листа языка в <c>PDATA.xlsx</c>.</summary>
    private static readonly string[] TextColumns = ["idx", "str"];

    /// <summary>
    /// Построить разделы. Данные ПЛК прочитаны и поддерживаются (<paramref name="plc"/> не <c>null</c> и не
    /// <see cref="PlcData.IsUnsupported"/>) — теги и дискретные алармы строятся по их сопоставлению, «Сводка» — с проектом
    /// ПЛК и строкой «Данные ПЛК» из <see cref="PlcData.Source"/>, как в консоли. Иначе теги сопоставляются без проекта
    /// ПЛК (<see cref="TagMatcher.Match"/> с <c>null</c>): колонки ПЛК — «неизвестно», данные ПЛК — «не задан». В «Сводке»
    /// модель панели и версия Runtime — «неизвестно» (файлы рядом с <c>pdata.fwc</c> окно не читает). Раздел, таблицу
    /// которого ядро построить не смогло, остаётся в дереве с пустой таблицей и причиной в <see cref="PanelSection.Error"/>.
    /// </summary>
    /// <returns>Разделы для дерева проекта.</returns>
    public static PanelView From(FwxDocument doc, string path, PlcData? plc)
    {
        var used = plc is { IsUnsupported: false } ? plc : null;
        // сопоставление нужно тегам и дискретным алармам; Lazy считает его один раз и повторяет его ошибку обоим
        var matches = new Lazy<IReadOnlyList<PlcMatch?>>(() => used?.Matches ?? TagMatcher.Match(doc, null));
        return new PanelView(
            Summary(doc, path, used),
            Tags(doc, matches),
            Alarms(doc, matches),
            Section("Screens", "Screens", ScreensSheet.Name, "folder", () => ScreensTable.Build(doc)),
            Texts(doc));
    }

    /// <summary>Сообщения о разделах, которые не построены, для вкладки Info.</summary>
    /// <returns>По строке «Раздел «X» не построен: причина» на раздел в порядке дерева; нет таких — пусто.</returns>
    public static IReadOnlyList<string> Errors(PanelView view) =>
    [
        .. new[] { view.Summary, view.Tags }.Concat(view.Alarms).Append(view.Screens).Concat(view.Texts)
            .Where(s => s.Error is not null)
            .Select(s => $"Раздел «{s.Title}» не построен: {s.Error}")
    ];

    /// <summary>«Сводка» — строки <see cref="SummaryRows.Build"/> с проектом ПЛК из <paramref name="plc"/>; <c>null</c> — без него.</summary>
    /// <returns>Раздел «Сводка».</returns>
    private static PanelSection Summary(FwxDocument doc, string path, PlcData? plc) =>
        Section("Сводка", "Сводка", SummarySheet.Name, "summary", () =>
        {
            var rows = SummaryRows.Build(doc, plc?.Project, new RunSummary(path, new PanelFiles(null, null), plc?.Source ?? NoPlc));
            return new ReportTable(SummaryColumns, [.. rows.Select(r => new ReportCell[] { r.Label, r.Value })]);
        }, isSummary: true);

    /// <summary>HMI tags — таблица листа «Теги» и строки статистики над ней.</summary>
    /// <returns>Раздел HMI tags.</returns>
    private static PanelSection Tags(FwxDocument doc, Lazy<IReadOnlyList<PlcMatch?>> matches)
    {
        try
        {
            return new PanelSection("HMI tags", "HMI tags", TagsSheet.Name, "tags", TagsTable.Build(doc, matches.Value),
                TagStatistics.Rows(doc, matches.Value));
        }
        catch (Exception e)
        {
            return Failed("HMI tags", "HMI tags", TagsSheet.Name, "tags", e);
        }
    }

    /// <summary>
    /// Разделы папки HMI alarms в порядке дерева: Discrete alarms, Analog alarms, System events, Alarm classes, Alarm groups.
    /// Раздел есть, только если в книге есть его лист (те же условия, что у <see cref="PanelDataExporter.Export"/>);
    /// упало и условие — раздел показывается с ошибкой.
    /// </summary>
    /// <returns>Разделы алармов; нет ни одного — пусто.</returns>
    private static List<PanelSection> Alarms(FwxDocument doc, Lazy<IReadOnlyList<PlcMatch?>> matches) =>
    [
        .. new[]
        {
            Alarm("Discrete alarms", DiscreteAlarmsSheet.Name, "alarm", () => doc.DiscreteAlarms.Count > 0, () => DiscreteAlarmsTable.Build(doc, matches.Value)),
            Alarm("Analog alarms", AnalogAlarmsSheet.Name, "alarm", () => doc.AnalogAlarms.Count > 0, () => AnalogAlarmsTable.Build(doc)),
            Alarm("System events", SystemEventsSheet.Name, "events", () => doc.SystemEvents.Count > 0, () => SystemEventsTable.Build(doc)),
            Alarm("Alarm classes", AlarmClassesSheet.Name, "classes", () => AlarmClassesTable.Classes(doc).Count > 0, () => AlarmClassesTable.Build(doc)),
            Alarm("Alarm groups", AlarmGroupsSheet.Name, "groups", () => AlarmGroupsTable.Groups(doc).Count > 0, () => AlarmGroupsTable.Build(doc))
        }.OfType<PanelSection>()
    ];

    /// <summary>Раздел папки HMI alarms с путём <c>HMI alarms ▸ title</c>, если <paramref name="exists"/>.</summary>
    /// <returns>Раздел; листа нет — <c>null</c>.</returns>
    private static PanelSection? Alarm(string title, string sheet, string icon, Func<bool> exists, Func<ReportTable> table)
    {
        var path = $"{AlarmsFolder}{Separator}{title}";
        try
        {
            return exists() ? new PanelSection(title, path, sheet, icon, table(), []) : null;
        }
        catch (Exception e)
        {
            return Failed(title, path, sheet, icon, e);
        }
    }

    /// <summary>
    /// Тексты: по разделу на язык в порядке листов <c>PDATA.xlsx</c>, колонки idx и str, строки в порядке файла. Сбой —
    /// один раздел «Тексты» с ошибкой.
    /// </summary>
    /// <returns>Разделы текстов; строк нет — пусто.</returns>
    private static List<PanelSection> Texts(FwxDocument doc)
    {
        try
        {
            return
            [
                .. doc.Strings.GroupBy(s => s.Lcid).Select(lang => new PanelSection(
                    LanguageText.Format(lang.Key),
                    $"{TextsFolder}{Separator}{LanguageText.Format(lang.Key)}",
                    $"0x{lang.Key:x}",
                    "text",
                    new ReportTable(TextColumns, [.. lang.Select(s => new ReportCell[] { s.Idx, s.Str })]),
                    []))
            ];
        }
        catch (Exception e)
        {
            return [Failed(TextsFolder, TextsFolder, "PDATA.xlsx", "text", e)];
        }
    }

    /// <summary>Раздел без статистики; таблица не построилась — раздел с ошибкой.</summary>
    /// <returns>Раздел.</returns>
    private static PanelSection Section(string title, string path, string sheet, string icon, Func<ReportTable> table, bool isSummary = false)
    {
        try
        {
            return new PanelSection(title, path, sheet, icon, table(), [], isSummary);
        }
        catch (Exception e)
        {
            return Failed(title, path, sheet, icon, e) with { IsSummary = isSummary };
        }
    }

    /// <summary>Раздел, таблицу которого построить не удалось: пустая таблица и причина (<see cref="ReadFailure.Describe"/>).</summary>
    /// <returns>Раздел с ошибкой.</returns>
    private static PanelSection Failed(string title, string path, string sheet, string icon, Exception e) =>
        new(title, path, sheet, icon, new ReportTable([], []), [], Error: ReadFailure.Describe(e));
}
