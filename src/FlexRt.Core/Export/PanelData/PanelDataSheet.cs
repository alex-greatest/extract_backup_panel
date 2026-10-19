using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Лист <c>panel_data.xlsx</c> в описании книги (<see cref="PanelDataBook.Sheets"/>).</summary>
/// <param name="Name">Имя листа: <c>Сводка</c>, <c>Теги</c>, <c>Дискретные алармы</c>.</param>
/// <param name="IsSummary">Раскладка «Сводки» (<see cref="ReportSheet.IsSummary"/>).</param>
/// <param name="Exists">Есть ли лист в книге для этой панели.</param>
/// <param name="Table">Таблица листа.</param>
/// <param name="Statistics">Строки статистики над таблицей — только у «Тегов»; у остальных пусто.</param>
public sealed record PanelDataSheet(
    string Name,
    bool IsSummary,
    Func<FwxDocument, bool> Exists,
    Func<PanelDataSource, ReportTable> Table,
    Func<PanelDataSource, IReadOnlyList<(string Label, int Count)>> Statistics);
