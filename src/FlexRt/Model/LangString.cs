namespace FlexRt.Model;

/// <summary>Строка из STRINGSTORE.</summary>
/// <param name="Idx">Номер строки в словаре языка, с единицы.</param>
/// <param name="Lcid">Код языка (0x409 — English, 0x407 — German, ...).</param>
/// <param name="Str">Текст без завершающего нуля.</param>
/// <param name="Offset">Смещение строки от начала блока строк, без старшего бита-флага.</param>
public sealed record LangString(int Idx, int Lcid, string Str, long Offset);
