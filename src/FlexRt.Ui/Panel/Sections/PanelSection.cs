using FlexRt.Export;

namespace FlexRt.Ui.Panel.Sections;

/// <summary>Раздел панели для окна: таблица одного листа Excel (или одного языка книги текстов) и строки статистики над ней.</summary>
/// <param name="Title">Имя узла в дереве проекта, как в TIA: <c>HMI tags</c>, <c>Discrete alarms</c>.</param>
/// <param name="Path">Путь раздела в заголовке таблицы после имени файла: <c>HMI alarms  ▸  Discrete alarms</c>.</param>
/// <param name="Sheet">Имя листа Excel, из которого таблица: <c>Теги</c>, <c>0x409</c>.</param>
/// <param name="Icon">Значок узла дерева для <c>Icon</c>.</param>
/// <param name="Table">Таблица листа — та же, что пишется в Excel.</param>
/// <param name="Statistics">Строки статистики над таблицей (у тегов — как вверху листа «Теги»); нет — пусто.</param>
/// <param name="IsSummary">«Сводка»: показывается списком «подпись — значение», без заголовков, фильтров и свойств строк.</param>
/// <param name="Error">Причина, по которой таблицу построить не удалось (таблица тогда пустая); <c>null</c> — построена.</param>
public sealed record PanelSection(
    string Title,
    string Path,
    string Sheet,
    string Icon,
    ReportTable Table,
    IReadOnlyList<(string Label, int Count)> Statistics,
    bool IsSummary = false,
    string? Error = null);
