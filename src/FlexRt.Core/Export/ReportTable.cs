namespace FlexRt.Export;

/// <summary>Таблица отчёта: заголовки колонок и строки значений, без привязки к Excel.</summary>
/// <param name="Columns">Заголовки колонок слева направо.</param>
/// <param name="Rows">Строки; в строке по ячейке на колонку.</param>
public sealed record ReportTable(IReadOnlyList<string> Columns, IReadOnlyList<ReportCell[]> Rows)
{
    /// <summary>
    /// Язык колонки по её номеру: LCID у колонки текста на одном языке панели, <c>null</c> — колонка не языковая;
    /// пусто — языковых колонок нет. В Excel не пишется: по нему окно показывает колонки выбранного языка. Задаётся только
    /// через <see cref="WithLanguages"/> — там по числу и порядку совпадает с <see cref="Columns"/>; выражение <c>with</c>
    /// с новыми <see cref="Columns"/> его не пересчитывает, поэтому таблицы с другими колонками строятся заново.
    /// </summary>
    public IReadOnlyList<int?> ColumnLanguages { get; private init; } = [];

    /// <summary>
    /// Группа колонки текста на языках по её номеру (<see cref="ReportColumn.Group"/>: <c>Tooltip</c>); <c>null</c> — не в
    /// группе; пусто — таблица без <see cref="WithLanguages"/>. В Excel не пишется: по нему окно собирает колонки одного
    /// текста на разных языках. Задаётся, как <see cref="ColumnLanguages"/>, только через <see cref="WithLanguages"/>.
    /// </summary>
    public IReadOnlyList<string?> ColumnGroups { get; private init; } = [];

    /// <summary>
    /// Таблица с колонками текстов на языках: заголовок и язык каждой колонки берутся из одной <see cref="ReportColumn"/>,
    /// поэтому языки не сдвигаются относительно заголовков.
    /// </summary>
    /// <returns>Таблица с <see cref="Columns"/>, <see cref="ColumnLanguages"/> и <see cref="ColumnGroups"/>.</returns>
    public static ReportTable WithLanguages(IReadOnlyList<ReportColumn> columns, IReadOnlyList<ReportCell[]> rows) =>
        new([.. columns.Select(c => c.Title)], rows)
        {
            ColumnLanguages = [.. columns.Select(c => c.Language)],
            ColumnGroups = [.. columns.Select(c => c.Group)]
        };
}
