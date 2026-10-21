namespace FlexRt.Export;

/// <summary>Колонка таблицы отчёта вместе с языком для <see cref="ReportTable.WithLanguages"/>.</summary>
/// <param name="Title">Заголовок колонки.</param>
/// <param name="Language">Язык (LCID) колонки текста на одном языке панели; <c>null</c> — колонка не языковая.</param>
public sealed record ReportColumn(string Title, int? Language = null);
