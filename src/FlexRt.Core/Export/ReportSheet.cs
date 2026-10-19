namespace FlexRt.Export;

/// <summary>Лист книги, построенный из готовой таблицы: имя, раскладка, таблица и строки статистики.</summary>
/// <param name="Name">Имя листа: <c>Сводка</c>, <c>Теги</c>, <c>Дискретные алармы</c>.</param>
/// <param name="Kind">Раскладка листа.</param>
/// <param name="Table">Таблица листа; у <see cref="ReportSheetKind.Summary"/> — две колонки: подпись и значение.</param>
/// <param name="Statistics">Строки статистики над таблицей — только у <see cref="ReportSheetKind.Tags"/>; иначе пусто.</param>
public sealed record ReportSheet(string Name, ReportSheetKind Kind, ReportTable Table, IReadOnlyList<(string Label, int Count)> Statistics);
