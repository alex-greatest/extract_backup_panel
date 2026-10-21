using FlexRt.Export;
using FlexRt.Export.PanelData;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>
/// Листы <c>panel_data.xlsx</c> из разделов окна: те же таблицы, что показаны, в порядке листов книги
/// (<see cref="PanelDataBook"/>). Листа «Ошибки» нет (решение пользователя: ошибки — в окне).
/// </summary>
public static class PanelDataSheets
{
    /// <summary>
    /// Листы всей книги: разделы <see cref="PanelView.Book"/> в их порядке — листы, которые есть в книге
    /// (<see cref="PanelDataBook"/>). Раздел, который не построен, не пишется.
    /// </summary>
    /// <returns>Листы по порядку книги и имена листов, пропущенных из-за непостроенных разделов.</returns>
    public static (IReadOnlyList<ReportSheet> Sheets, IReadOnlyList<string> Skipped) Book(PanelView view) =>
        ([.. view.Book.Where(s => s.Error is null).Select(Sheet)], [.. view.Book.Where(s => s.Error is not null).Select(s => s.Sheet)]);

    /// <summary>Можно ли сохранить раздел отдельным листом: это построенный раздел <c>panel_data.xlsx</c>, а не язык текстов.</summary>
    /// <returns><c>true</c> для построенного раздела книги данных панели.</returns>
    public static bool IsSheet(PanelView view, object? selected) =>
        selected is PanelSection { Error: null } section && !view.Texts.Contains(section);

    /// <summary>Лист раздела — тот, что вернуло ядро (<see cref="PanelSection.Report"/>).</summary>
    /// <returns>Лист.</returns>
    public static ReportSheet Sheet(PanelSection section) => section.Report;
}
