namespace FlexRt.Export;

/// <summary>Правила записи текста в ячейку Excel, общие для всех экспортёров в XLSX.</summary>
internal static class ExcelCell
{
    /// <summary>Предел длины текста в ячейке Excel.</summary>
    private const int MaxLength = 32767;

    /// <summary>
    /// Текст для <c>SetValue</c> ячейки: строка длиннее предела <see cref="MaxLength"/> обрезается.
    /// Текст с апострофом в начале получает второй апостроф: ClosedXML убирает первый и ставит
    /// ячейке префикс текста (<c>quotePrefix</c>), поэтому в Excel виден исходный текст с апострофом
    /// (<c>'Project ID' area pointer…</c>). ClosedXML проверяет предел длины до того, как убирает
    /// апостроф, поэтому такой текст обрезается на символ короче — до <c>MaxLength - 1</c>.
    /// </summary>
    /// <returns>Текст для записи в ячейку, не длиннее <see cref="MaxLength"/>.</returns>
    public static string Text(string value)
    {
        if (!value.StartsWith('\''))
        {
            return value.Length > MaxLength ? value[..MaxLength] : value;
        }
        return "'" + (value.Length > MaxLength - 1 ? value[..(MaxLength - 1)] : value);
    }
}
