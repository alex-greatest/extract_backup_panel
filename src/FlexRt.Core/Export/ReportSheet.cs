namespace FlexRt.Export;

/// <summary>
/// Лист книги из готовой таблицы. Раскладка следует из данных: <paramref name="IsSummary"/> — как лист «Сводка» (подпись в
/// A, значение в B, без заголовков); есть <paramref name="Statistics"/> — как лист «Теги» (статистика сверху, таблица со
/// строки заголовка листа «Теги»); иначе — таблица с первой строки под именем <paramref name="Name"/>.
/// </summary>
/// <param name="Name">Имя листа: <c>Сводка</c>, <c>Теги</c>, <c>Дискретные алармы</c>.</param>
/// <param name="Table">Таблица листа; у «Сводки» — две колонки: подпись и значение.</param>
/// <param name="Statistics">Строки статистики над таблицей — только у «Тегов»; иначе пусто.</param>
/// <param name="IsSummary">Лист «Сводка».</param>
public sealed record ReportSheet(string Name, ReportTable Table, IReadOnlyList<(string Label, int Count)> Statistics, bool IsSummary);
