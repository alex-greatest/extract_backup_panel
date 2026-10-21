using System.Globalization;
using FlexRt.Model.Panel;

namespace FlexRt.Export;

/// <summary>Код языка панели (LCID) в виде для отчёта.</summary>
public static class LanguageText
{
    /// <summary>
    /// Языки панели (LCID) в порядке STRINGSTORE, без повторов: порядок колонок текстов на листах и языков в окне.
    /// </summary>
    /// <returns>Языки; строк нет — пусто.</returns>
    public static IReadOnlyList<int> Languages(FwxDocument doc) => [.. doc.Strings.Select(s => s.Lcid).Distinct()];

    /// <summary>
    /// Колонки текста по языкам <paramref name="languages"/>: заголовок — язык (<see cref="Format"/>), перед ним
    /// <paramref name="prefix"/> через пробел, если задан (<c>Display name 0x409 — English (United States)</c>).
    /// </summary>
    /// <returns>По колонке на язык.</returns>
    public static IEnumerable<ReportColumn> Columns(IEnumerable<int> languages, string? prefix = null) =>
        languages.Select(lcid => new ReportColumn(prefix is null ? Format(lcid) : $"{prefix} {Format(lcid)}", lcid));

    /// <summary>
    /// Код и название языка на нём самом: <c>0x419 — русский (Россия)</c>,
    /// <c>0x409 — English (United States)</c>. Название не зависит от языка Windows, на которой
    /// запущена программа. Код, который .NET не знает, — только код (<c>0x1234</c>).
    /// </summary>
    /// <returns>Текст ячейки.</returns>
    public static string Format(int lcid)
    {
        var code = $"0x{lcid:x}";
        try
        {
            return $"{code} — {CultureInfo.GetCultureInfo(lcid).NativeName}";
        }
        catch (Exception e) when (e is CultureNotFoundException or ArgumentOutOfRangeException)
        {
            return code;
        }
    }
}
