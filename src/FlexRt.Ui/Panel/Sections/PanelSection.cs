using FlexRt.Export;

namespace FlexRt.Ui.Panel.Sections;

/// <summary>Раздел панели для окна: лист Excel (или один язык книги текстов) и место его узла в дереве.</summary>
/// <param name="Title">Имя узла в дереве проекта, как в TIA: <c>HMI tags</c>, <c>Discrete alarms</c>.</param>
/// <param name="Path">Путь раздела в заголовке таблицы после имени файла: <c>HMI alarms  ▸  Discrete alarms</c>.</param>
/// <param name="Icon">Значок узла дерева для <c>Icon</c>.</param>
/// <param name="Report">Лист — тот же, что пишется в Excel; не построен — пустой лист с тем же именем.</param>
/// <param name="Error">Причина, по которой таблицу построить не удалось (таблица тогда пустая); <c>null</c> — построена.</param>
public sealed record PanelSection(string Title, string Path, string Icon, ReportSheet Report, string? Error = null)
{
    /// <summary>Имя листа Excel, из которого таблица: <c>Теги</c>, <c>0x409</c>.</summary>
    public string Sheet => Report.Name;

    /// <summary>Таблица листа.</summary>
    public ReportTable Table => Report.Table;

    /// <summary>Строки статистики над таблицей (у тегов — как вверху листа «Теги»); нет — пусто.</summary>
    public IReadOnlyList<(string Label, int Count)> Statistics => Report.Statistics;

    /// <summary>«Сводка»: показывается списком «подпись — значение», без заголовков, фильтров и свойств строк.</summary>
    public bool IsSummary => Report.IsSummary;
}
