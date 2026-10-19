using FlexRt.Export;
using FlexRt.Export.PanelData;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>
/// Листы <c>panel_data.xlsx</c> из разделов окна: те же таблицы, что показаны, в порядке листов книги консоли. Листа
/// «Ошибки» нет (решение пользователя: ошибки — в окне).
/// </summary>
public static class PanelDataSheets
{
    /// <summary>
    /// Листы всей книги: «Сводка», «Теги» (и без тегов — решение пользователя «писать всё, что есть»), листы алармов,
    /// которые есть в дереве, «Экраны», если экраны есть (как в консоли). Раздел, который не построен, не пишется.
    /// </summary>
    /// <returns>Листы по порядку книги и имена листов, пропущенных из-за непостроенных разделов.</returns>
    public static (IReadOnlyList<ReportSheet> Sheets, IReadOnlyList<string> Skipped) Book(PanelView view)
    {
        List<PanelSection> sections = [view.Summary, view.Tags, .. view.Alarms];
        // в консоли лист «Экраны» есть, только если экраны есть; строк таблицы столько же, сколько экранов
        if (view.Screens.Error is not null || view.Screens.Table.Rows.Count > 0)
        {
            sections.Add(view.Screens);
        }
        // в дереве алармы идут в другом порядке, чем листы книги
        sections = [.. sections.OrderBy(s => BookPosition(s.Sheet))];
        return ([.. sections.Where(s => s.Error is null).Select(Sheet)],
            [.. sections.Where(s => s.Error is not null).Select(s => s.Sheet)]);
    }

    /// <summary>Место листа в книге консоли (<see cref="PanelDataExporter.SheetOrder"/>); неизвестный лист — после всех.</summary>
    /// <returns>Номер листа в порядке книги с нуля.</returns>
    private static int BookPosition(string sheet) =>
        PanelDataExporter.SheetOrder.ToList().IndexOf(sheet) is var i and >= 0 ? i : PanelDataExporter.SheetOrder.Count;

    /// <summary>Можно ли сохранить раздел отдельным листом: это построенный раздел <c>panel_data.xlsx</c>, а не язык текстов.</summary>
    /// <returns><c>true</c> для построенного раздела книги данных панели.</returns>
    public static bool IsSheet(PanelView view, object? selected) =>
        selected is PanelSection { Error: null } section && !view.Texts.Contains(section);

    /// <summary>Лист раздела: раскладка «Сводки», «Тегов» (статистика есть только у них) или таблица с первой строки.</summary>
    /// <returns>Лист.</returns>
    public static ReportSheet Sheet(PanelSection section) => new(section.Sheet, section.Table, section.Statistics, section.IsSummary);
}
