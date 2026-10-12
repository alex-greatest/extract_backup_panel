namespace FlexRt.Extraction;

/// <summary>Итог одного экспорта.</summary>
/// <param name="Title">Название экспорта: <c>Языки -> XLSX</c>, <c>Теги -> XLSX</c>, <c>Explode -> HEX</c>.</param>
/// <param name="Result">Итог при успехе (<c>16 тегов -> путь</c> или сообщение о пропуске); при ошибке — <c>null</c>.</param>
/// <param name="Error">Ошибка экспорта; при успехе — <c>null</c>.</param>
/// <param name="ElapsedMs">Время экспорта в миллисекундах.</param>
public sealed record ExportOutcome(string Title, string? Result, Exception? Error, long ElapsedMs);
