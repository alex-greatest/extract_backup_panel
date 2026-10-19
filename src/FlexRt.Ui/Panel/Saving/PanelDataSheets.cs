using FlexRt.Export;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>
/// Листы <c>panel_data.xlsx</c> из разделов окна: те же таблицы, что показаны, в порядке листов книги консоли. Листа
/// «Ошибки» нет (решение пользователя: ошибки — в окне).
/// </summary>
public static class PanelDataSheets
{
    /// <summary>Порядок листов алармов в книге — как у <see cref="FlexRt.Export.PanelData.PanelDataExporter.Export"/> (в дереве порядок другой).</summary>
    private static readonly string[] AlarmSheetOrder = ["Системные события", "Дискретные алармы", "Аналоговые алармы", "Классы алармов", "Группы алармов"];

    /// <summary>
    /// Листы всей книги: «Сводка», «Теги» (и без тегов — решение пользователя «писать всё, что есть»), листы алармов,
    /// которые есть в дереве, «Экраны», если экраны есть (как в консоли). Раздел, который не построен, не пишется.
    /// </summary>
    /// <returns>Листы по порядку книги и имена листов, пропущенных из-за непостроенных разделов.</returns>
    public static (IReadOnlyList<ReportSheet> Sheets, IReadOnlyList<string> Skipped) Book(PanelView view)
    {
        List<PanelSection> sections = [view.Summary, view.Tags, .. view.Alarms.OrderBy(a => Array.IndexOf(AlarmSheetOrder, a.Sheet))];
        // в консоли лист «Экраны» есть, только если экраны есть; строк таблицы столько же, сколько экранов
        if (view.Screens.Error is not null || view.Screens.Table.Rows.Count > 0)
        {
            sections.Add(view.Screens);
        }
        return ([.. sections.Where(s => s.Error is null).Select(Sheet)],
            [.. sections.Where(s => s.Error is not null).Select(s => s.Sheet)]);
    }

    /// <summary>Можно ли сохранить раздел отдельным листом: это построенный раздел <c>panel_data.xlsx</c>, а не язык текстов.</summary>
    /// <returns><c>true</c> для построенного раздела книги данных панели.</returns>
    public static bool IsSheet(PanelView view, object? selected) =>
        selected is PanelSection { Error: null } section && !view.Texts.Contains(section);

    /// <summary>Лист раздела: раскладка «Сводки», «Тегов» (статистика есть только у них) или таблица с первой строки.</summary>
    /// <returns>Лист.</returns>
    public static ReportSheet Sheet(PanelSection section) => new(section.Sheet, section.Table, section.Statistics, section.IsSummary);
}
