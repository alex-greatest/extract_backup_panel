namespace FlexRt.Export;

/// <summary>Правила записи текста в ячейку Excel, общие для всех экспортёров в XLSX.</summary>
internal static class ExcelCell
{
    /// <summary>Предел длины текста в ячейке Excel.</summary>
    private const int MaxLength = 32767;

    /// <summary>Текст для ячейки: строка длиннее предела <see cref="MaxLength"/> обрезается.</summary>
    /// <returns>Исходный или обрезанный текст.</returns>
    public static string Text(string value) => value.Length > MaxLength ? value[..MaxLength] : value;
}
