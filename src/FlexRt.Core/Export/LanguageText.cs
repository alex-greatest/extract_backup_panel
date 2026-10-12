using System.Globalization;

namespace FlexRt.Export;

/// <summary>Код языка панели (LCID) в виде для отчёта.</summary>
internal static class LanguageText
{
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
