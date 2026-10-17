namespace FlexRt.Ui.Components.Sections;

/// <summary>Колонка таблицы раздела в <c>ReportGrid</c>.</summary>
/// <param name="Index">Номер колонки в таблице листа с нуля.</param>
/// <param name="Key">Ключ значения в строке таблицы: <c>c0</c>, <c>c1</c>… — заголовок листа ключом не годится (кавычки, пробелы).</param>
/// <param name="Title">Заголовок колонки листа.</param>
/// <param name="IsNumber">Все непустые ячейки колонки — числа: сортировка и фильтр по числу, выравнивание вправо.</param>
public sealed record GridColumn(int Index, string Key, string Title, bool IsNumber)
{
    /// <summary>Тип значения колонки для Radzen: <c>long?</c> у числовой, <c>string</c> у текстовой.</summary>
    public Type ValueType => IsNumber ? typeof(long?) : typeof(string);
}
