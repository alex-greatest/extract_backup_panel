namespace FlexRt.Export;

/// <summary>
/// Выбор языка таблиц по языкам панели (LCID в порядке STRINGSTORE): <c>null</c> — все языки, иначе LCID языка, колонки
/// которого показаны.
/// </summary>
public static class LanguageChoice
{
    /// <summary>Язык после открытия файла: первый язык панели; языков нет — <c>null</c> (все языки).</summary>
    /// <returns>LCID первого языка или <c>null</c>.</returns>
    public static int? First(IReadOnlyList<int> languages) => languages is [var first, ..] ? first : null;

    /// <summary>
    /// Язык после перечитывания той же панели: прежний <paramref name="current"/>, если он есть у панели или выбраны все
    /// языки (<c>null</c>); иначе <see cref="First"/>.
    /// </summary>
    /// <returns>LCID языка или <c>null</c>.</returns>
    public static int? Kept(IReadOnlyList<int> languages, int? current) =>
        current is null || languages.Contains(current.Value) ? current : First(languages);
}
