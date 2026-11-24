using FlexRt.Model.Panel;

namespace FlexRt.Export.ScreenObjects;

/// <summary>
/// Подсказки I/O field и VIEW_BUTTONS на языках панели: пары INFO_HANDLER с элементом объекта.
/// Сверено на шагах 53 для I/O field и 12 для кнопки шаблона (Project1, V21).
/// </summary>
public sealed class ScreenObjectTooltips
{
    /// <summary>Номер строки языков подсказки по элементу объекта.</summary>
    private readonly Dictionary<VarReference, int> _textIndexes;

    /// <summary>Строки языков панели.</summary>
    private readonly LanguageStrings _strings;

    /// <summary>Собрать подсказки полей и кнопок; у объекта с несколькими парами берётся первая.</summary>
    public ScreenObjectTooltips(FwxDocument doc)
    {
        var owners = doc.IoFields.Select(f => f.Element)
            .Concat(doc.ScreenObjects.Where(o => o.Table == "VIEW_BUTTONS").Select(o => o.Element))
            .ToHashSet();
        _textIndexes = doc.InfoTexts
            .Where(t => owners.Contains(t.Owner))
            .DistinctBy(t => t.Owner)
            .ToDictionary(t => t.Owner, t => t.TextIndex);
        _strings = LanguageStrings.Of(doc);
    }

    /// <summary>Подсказка объекта на всех языках Runtime в порядке STRINGSTORE, без изменения текста.</summary>
    /// <returns>Язык и текст; пары INFO_HANDLER нет или строки на языке нет — текст пустой.</returns>
    public IReadOnlyList<(int Language, string Text)> Texts(VarReference owner) =>
        [.. _strings.Languages.Select(language => (language, Text(owner, language)))];

    /// <summary>
    /// Текст подсказки объекта как есть. Язык не выбран («Все языки») или такого языка в панели нет — первый язык панели, как у
    /// Tooltip экрана.
    /// </summary>
    /// <param name="owner">Элемент объекта.</param>
    /// <param name="language">LCID выбранного языка или <c>null</c>.</param>
    /// <returns>Текст; подсказки нет или нет строки на языке — пусто.</returns>
    public string Text(VarReference owner, int? language)
    {
        if (!_textIndexes.TryGetValue(owner, out var index) || _strings.Languages is [])
        {
            return "";
        }
        return _strings.Text(LanguageChoice.Kept(_strings.Languages, language) ?? _strings.Languages[0], index);
    }
}
