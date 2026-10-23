using FlexRt.Export;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

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
    /// (<see cref="FwxDocument.StartScreenNumber"/>). Служебные записи pop-up не входят. Высота на холсте — высота ядра
    /// (<see cref="ScreensTable.Screens"/>, та же, что в колонке Height); над обычными экранами и экранами
    /// неопознанного вида — полоса Permanent area (<see cref="Strip"/>), как в редакторе экранов TIA.
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
        var area = screens.FirstOrDefault(s => s.Kind == ScreenKind.PermanentArea).Screen;
        var strip = Strip(doc, area);
        return
        [
            .. screens.Zip(table.Rows, (s, row) => new ScreenItem(
                s.Screen,
                row[nameColumn].Text ?? "",
                s.Kind,
                s.Number is not null && s.Number == doc.StartScreenNumber,
                Properties(table, row),
                s.Height,
                strip is not null && UnderPermanentArea(s.Kind, s.Screen, area, strip) ? strip : null))
        ];
    }

    /// <summary>
    /// Полоса Permanent area над обычными экранами: высота <see cref="FwxDocument.PermanentAreaHeight"/>, цвет фона и
    /// высота записи Permanent area (запись <c>Overview</c> — вся панель).
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="area">Запись Permanent area из строк листа; её нет — полоса без цвета и высоты панели.</param>
    /// <returns>Полоса; высота не прочитана или 0 — <c>null</c>.</returns>
    private static PermanentStrip? Strip(FwxDocument doc, Screen? area) =>
        doc.PermanentAreaHeight is > 0 and var height
            ? new PermanentStrip(height, area?.BackgroundRgb, area?.Height)
            : null;

    /// <summary>
    /// Экран под полосой Permanent area: обычный (<see cref="ScreenKind.Screen"/>) и неопознанного вида, если по размеру
    /// это обычный экран — ширина панели и высота панели без Permanent area (размер панели — запись Permanent area).
    /// Так обычный экран с непрочитанным номером (ADAPTVALUECHANG не целиком) остаётся с полосой, а pop-up или
    /// slide-in, которые не опознаны (служебная запись или имя не прочитаны), — без неё (решение пользователя 2026-10-23).
    /// </summary>
    /// <param name="kind">Вид экрана.</param>
    /// <param name="screen">Запись SCREEN экрана.</param>
    /// <param name="area">Запись Permanent area; её нет — неопознанный экран без полосы.</param>
    /// <param name="strip">Полоса Permanent area.</param>
    /// <returns><c>true</c> — над экраном рисуется полоса.</returns>
    private static bool UnderPermanentArea(string kind, Screen screen, Screen? area, PermanentStrip strip) =>
        kind == ScreenKind.Screen
        || ScreenKind.IsUnrecognized(kind) && area is not null && screen.Width == area.Width && screen.Height + strip.Height == area.Height;

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
