using FlexRt.Export;

namespace FlexRt.Ui.Components.Sections;

/// <summary>
/// Таблица листа в виде, который понимает <c>RadzenDataGrid</c>: строка — словарь «ключ колонки — значение». Радзен
/// сортирует и фильтрует по типу значения, поэтому числовая колонка хранит <c>long?</c>, текстовая — строку.
/// </summary>
public static class GridData
{
    /// <summary>Ключ номера строки листа в словаре строки: по нему строка находится в таблице для заливки и свойств.</summary>
    public const string RowKey = "row";

    /// <summary>
    /// Колонки таблицы; колонка числовая, если в ней есть число, а остальные ячейки — пустой текст; язык колонки — из
    /// <see cref="ReportTable.ColumnLanguages"/> (не задан — <c>null</c>).
    /// </summary>
    /// <returns>Колонки слева направо.</returns>
    public static IReadOnlyList<GridColumn> Columns(ReportTable table) =>
    [
        .. table.Columns.Select((title, i) => new GridColumn(i, $"c{i}", title, table.ColumnLanguages.ElementAtOrDefault(i), IsNumber(table, i)))
    ];

    /// <summary>
    /// Строки для таблицы в порядке листа. Числовая колонка: число или <c>null</c> для пустой ячейки; текстовая — текст
    /// ячейки на языке <paramref name="language"/> (<see cref="ReportCell.DisplayIn"/>).
    /// </summary>
    /// <returns>Строки — словари со значениями колонок и <see cref="RowKey"/>.</returns>
    public static List<IDictionary<string, object>> Rows(ReportTable table, IReadOnlyList<GridColumn> columns, int? language) =>
    [
        .. table.Rows.Select((cells, i) =>
        {
            var row = new Dictionary<string, object> { [RowKey] = i };
            foreach (var column in columns)
            {
                // пустая ячейка числовой колонки — null: так её понимают сортировка и фильтр Radzen
                row[column.Key] = (column.IsNumber ? cells[column.Index].Number : cells[column.Index].DisplayIn(language))!;
            }
            return row;
        })
    ];

    /// <summary>
    /// Перевести строки на язык <paramref name="language"/>: в текстовых колонках ячейки с текстами на языках
    /// (<see cref="ReportCell.LanguageTexts"/>) получают текст этого языка — по нему сортируют и фильтруют.
    /// </summary>
    public static void SetLanguage(List<IDictionary<string, object>> rows, ReportTable table, IReadOnlyList<GridColumn> columns, int? language)
    {
        foreach (var row in rows)
        {
            var cells = table.Rows[(int)row[RowKey]];
            foreach (var column in columns.Where(c => !c.IsNumber && cells[c.Index].LanguageTexts is not null))
            {
                row[column.Key] = cells[column.Index].DisplayIn(language);
            }
        }
    }

    /// <summary>Колонка числовая: есть хотя бы одно число, а в остальных ячейках пустой текст.</summary>
    /// <returns><c>true</c> для числовой колонки.</returns>
    private static bool IsNumber(ReportTable table, int column) =>
        table.Rows.Any(r => r[column].Number is not null)
        && table.Rows.All(r => r[column].Number is not null || string.IsNullOrEmpty(r[column].Text));
}
