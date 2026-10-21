using FlexRt.Export;
using FlexRt.Export.PanelData;
using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Extraction;
using FlexRt.Model.Panel;

namespace FlexRt.Ui.Panel.Sections;

/// <summary>
/// Разделы прочитанной панели для окна. Листы, их таблицы и порядок берутся из описания книги ядра
/// (<see cref="PanelDataBook"/>). Разделы строятся независимо: сбой одного не мешает остальным и экранам (решение
/// пользователя).
/// </summary>
public static class PanelViews
{
    /// <summary>Разделитель частей пути в заголовке таблицы, как у экранов.</summary>
    private const string Separator = "  ▸  ";

    /// <summary>Папка алармов в дереве, как в TIA.</summary>
    private const string AlarmsFolder = "HMI alarms";

    /// <summary>Папка текстов в дереве.</summary>
    private const string TextsFolder = "Тексты";

    /// <summary>Колонки листа языка в <c>PDATA.xlsx</c>.</summary>
    private static readonly string[] TextColumns = ["idx", "str"];

    /// <summary>
    /// Узлы дерева для листов книги: имя как в TIA, значок и место в папке HMI alarms. Алармы — в порядке дерева, как в
    /// TIA: другой порядок, чем в книге.
    /// </summary>
    private static readonly (string Sheet, string Title, string Icon, bool InAlarms)[] Nodes =
    [
        (SummarySheet.Name, "Сводка", "summary", false),
        (TagsSheet.Name, "HMI tags", "tags", false),
        (DiscreteAlarmsSheet.Name, "Discrete alarms", "alarm", true),
        (AnalogAlarmsSheet.Name, "Analog alarms", "alarm", true),
        (SystemEventsSheet.Name, "System events", "events", true),
        (AlarmClassesSheet.Name, "Alarm classes", "classes", true),
        (AlarmGroupsSheet.Name, "Alarm groups", "groups", true),
        (ScreensSheet.Name, "Screens", "folder", false)
    ];

    /// <summary>
    /// Построить разделы. Разделы листов <c>panel_data.xlsx</c> — из описания книги ядра
    /// (<see cref="PanelDataBook.TryBuild(PanelDataSource)"/>): раздел есть, только если в книге есть его лист, каждый
    /// строится отдельно; данные листов с данными ПЛК и без них — <see cref="PanelSources.From"/>. Раздел, таблицу
    /// которого ядро построить не смогло, остаётся в дереве с пустой таблицей и причиной в <see cref="PanelSection.Error"/>.
    /// Листа «Экраны» нет (экранов нет) — раздел Screens пустой: папка Screens в дереве тогда не показывается.
    /// </summary>
    /// <returns>Разделы для дерева проекта.</returns>
    public static PanelView From(FwxDocument doc, string path, PlcData? plc)
    {
        var source = PanelSources.From(doc, path, plc);
        List<PanelSection> book = [.. PanelDataBook.TryBuild(source).Select(Section)];
        var bySheet = book.ToDictionary(s => s.Sheet);
        return new PanelView(
            bySheet[SummarySheet.Name],
            bySheet[TagsSheet.Name],
            [.. Nodes.Where(n => n.InAlarms).Select(n => bySheet.GetValueOrDefault(n.Sheet)).OfType<PanelSection>()],
            bySheet.GetValueOrDefault(ScreensSheet.Name) ?? Section(new BuiltSheet(Empty(ScreensSheet.Name), null)),
            Texts(doc),
            book,
            LanguageText.Languages(doc));
    }

    /// <summary>Сообщения о разделах, которые не построены, для вкладки Info.</summary>
    /// <returns>По строке «Раздел «X» не построен: причина» на раздел в порядке дерева; нет таких — пусто.</returns>
    public static IReadOnlyList<string> Errors(PanelView view) =>
    [
        .. new[] { view.Summary, view.Tags }.Concat(view.Alarms).Append(view.Screens).Concat(view.Texts)
            .Where(s => s.Error is not null)
            .Select(s => $"Раздел «{s.Title}» не построен: {s.Error}")
    ];

    /// <summary>Раздел листа книги: лист из ядра (не построен — пустой) и причина сбоя (<see cref="ReadFailure.Describe"/>).</summary>
    /// <returns>Раздел.</returns>
    private static PanelSection Section(BuiltSheet built)
    {
        var (title, path, icon) = Node(built.Sheet.Name);
        return new PanelSection(title, path, icon, built.Sheet, built.Error is { } e ? ReadFailure.Describe(e) : null);
    }

    /// <summary>
    /// Узел дерева для листа книги из <see cref="Nodes"/>: имя, путь в заголовке таблицы (у алармов —
    /// <c>HMI alarms ▸ имя</c>) и значок.
    /// </summary>
    /// <returns>Имя, путь и значок.</returns>
    /// <exception cref="InvalidOperationException">
    /// Листа нет в <see cref="Nodes"/>: новый лист ядра не попал в дерево — разделы не строятся, окно называет шаг.
    /// </exception>
    private static (string Title, string Path, string Icon) Node(string sheet)
    {
        foreach (var node in Nodes)
        {
            if (node.Sheet == sheet)
            {
                return (node.Title, node.InAlarms ? $"{AlarmsFolder}{Separator}{node.Title}" : node.Title, node.Icon);
            }
        }
        throw new InvalidOperationException($"у листа «{sheet}» нет узла в дереве окна");
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
                    "text",
                    new ReportSheet($"0x{lang.Key:x}", new ReportTable(TextColumns, [.. lang.Select(s => new ReportCell[] { s.Idx, s.Str })]),
                        [], false)))
            ];
        }
        catch (Exception e)
        {
            return [new PanelSection(TextsFolder, TextsFolder, "text", Empty("PDATA.xlsx"), ReadFailure.Describe(e))];
        }
    }

    /// <summary>Пустой лист-таблица: раздел без строк или раздел, который не построен.</summary>
    /// <returns>Лист без колонок и строк.</returns>
    private static ReportSheet Empty(string name) => new(name, new ReportTable([], []), [], false);
}
