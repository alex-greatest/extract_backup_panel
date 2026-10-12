namespace FlexRt.Export;

/// <summary>Таблица отчёта: заголовки колонок и строки значений, без привязки к Excel.</summary>
/// <param name="Columns">Заголовки колонок слева направо.</param>
/// <param name="Rows">Строки; в строке по ячейке на колонку.</param>
public sealed record ReportTable(IReadOnlyList<string> Columns, IReadOnlyList<ReportCell[]> Rows);
