using FlexRt.Export;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Model.Panel;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Экраны прочитанной панели для окна: строки листа «Экраны» вместе с записями SCREEN.</summary>
public static class ScreenItems
{
    /// <summary>Заголовок колонки Name в таблице листа «Экраны».</summary>
    private const string NameColumn = "Name";

    /// <summary>
    /// Экраны в порядке листа «Экраны» (Permanent area, обычные по номеру, остальные): строки
    /// <see cref="ScreensTable.Build"/> и записи <see cref="ScreensTable.Screens"/> идут в одном порядке, поэтому
    /// сводятся попарно. Имя берётся из колонки Name по её заголовку; стартовый — экран с номером из GUI
    /// (<see cref="FwxDocument.StartScreenNumber"/>). Служебные записи pop-up не входят.
    /// </summary>
    /// <returns>Экраны для дерева, холста и панели свойств; экранов нет — пустой список.</returns>
    /// <exception cref="InvalidOperationException">
    /// В таблице нет колонки Name или строк не столько, сколько экранов: ядро изменило таблицу листа «Экраны».
    /// </exception>
    public static IReadOnlyList<ScreenItem> From(FwxDocument doc)
    {
        var table = ScreensTable.Build(doc);
        var screens = ScreensTable.Screens(doc);
        if (table.Rows.Count != screens.Count)
        {
            throw new InvalidOperationException($"в таблице экранов {table.Rows.Count} строк, а экранов {screens.Count}");
        }
        var nameColumn = ColumnIndex(table, NameColumn);
        return
        [
            .. screens.Zip(table.Rows, (s, row) => new ScreenItem(
                s.Screen,
                row[nameColumn].Text ?? "",
                s.Kind,
                s.Number is not null && s.Number == doc.StartScreenNumber,
                Properties(table, row)))
        ];
    }

    /// <summary>
    /// Свойства экрана — ячейки строки под заголовками колонок; язык и группа колонки — из
    /// <see cref="ReportTable.ColumnLanguages"/> и <see cref="ReportTable.ColumnGroups"/> (пусто — языковых колонок нет).
    /// </summary>
    /// <returns>По свойству на колонку в порядке листа.</returns>
    private static List<ScreenProperty> Properties(ReportTable table, ReportCell[] row) =>
    [
        .. table.Columns.Select((title, i) => new ScreenProperty(
            title,
            row[i],
            table.ColumnLanguages.ElementAtOrDefault(i),
            table.ColumnGroups.ElementAtOrDefault(i)))
    ];

    /// <summary>Номер колонки таблицы по заголовку.</summary>
    /// <returns>Номер колонки с нуля.</returns>
    /// <exception cref="InvalidOperationException">Колонки с таким заголовком нет.</exception>
    private static int ColumnIndex(ReportTable table, string title)
    {
        var index = table.Columns.ToList().IndexOf(title);
        return index >= 0 ? index : throw new InvalidOperationException($"в таблице экранов нет колонки {title}");
    }
}
