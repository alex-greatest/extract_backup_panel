namespace FlexRt.Ui.Panel.Sections;

/// <summary>Разделы открытой панели для дерева проекта: листы <c>panel_data.xlsx</c> (с данными ПЛК, если они выбраны) и языки <c>PDATA.xlsx</c>.</summary>
/// <param name="Summary">«Сводка».</param>
/// <param name="Tags">HMI tags — лист «Теги».</param>
/// <param name="Alarms">Разделы папки HMI alarms: только те, чьи листы есть в книге.</param>
/// <param name="Screens">Таблица листа «Экраны» для папки Screens.</param>
/// <param name="Texts">Тексты: по разделу на язык, в порядке листов <c>PDATA.xlsx</c>.</param>
/// <param name="Book">
/// Разделы листов <c>panel_data.xlsx</c> в порядке книги — те же объекты, что в дереве: листы, которые есть в книге, и
/// те, что не построены.
/// </param>
public sealed record PanelView(
    PanelSection Summary,
    PanelSection Tags,
    IReadOnlyList<PanelSection> Alarms,
    PanelSection Screens,
    IReadOnlyList<PanelSection> Texts,
    IReadOnlyList<PanelSection> Book);
