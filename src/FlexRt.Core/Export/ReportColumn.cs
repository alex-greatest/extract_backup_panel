namespace FlexRt.Export;

/// <summary>Колонка таблицы отчёта вместе с языком и группой для <see cref="ReportTable.WithLanguages"/>.</summary>
/// <param name="Title">Заголовок колонки.</param>
/// <param name="Language">Язык (LCID) колонки текста на одном языке панели; <c>null</c> — колонка не языковая.</param>
/// <param name="Group">
/// Группа колонок текста на языках — начало их заголовков (<c>Tooltip</c>, <c>Display name</c>); <c>null</c> — колонка
/// не в группе (не языковая или текст без начала заголовка).
/// </param>
public sealed record ReportColumn(string Title, int? Language = null, string? Group = null);
