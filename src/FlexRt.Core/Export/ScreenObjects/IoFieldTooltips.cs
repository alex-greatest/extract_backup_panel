using FlexRt.Model.Panel;

namespace FlexRt.Export.ScreenObjects;

/// <summary>
/// Подсказки (Tooltip) I/O field на языках панели: пары INFO_HANDLER с элементом поля, текст — строка языков с номером из
/// пары, как есть. Сверено с TIA на шаге <c>data/real/steps-iofield/53_tooltip.fwc</c> (Project1, V21); у поля Output пары не встречались.
/// </summary>
public sealed class IoFieldTooltips
{
    /// <summary>Номер строки языков подсказки по элементу поля.</summary>
    private readonly Dictionary<VarReference, int> _textIndexes;

    /// <summary>Строки языков панели.</summary>
    private readonly LanguageStrings _strings;

    /// <summary>Собрать пары подсказок полей и строки документа; у записи несколько пар — берётся первая.</summary>
    public IoFieldTooltips(FwxDocument doc)
    {
        var fields = doc.IoFields.Select(f => f.Element).ToHashSet();
        _textIndexes = doc.InfoTexts
            .Where(t => fields.Contains(t.Owner))
            .DistinctBy(t => t.Owner)
            .ToDictionary(t => t.Owner, t => t.TextIndex);
        _strings = LanguageStrings.Of(doc);
    }

    /// <summary>
    /// Текст подсказки поля как есть. Язык не выбран («Все языки») или такого языка в панели нет — первый язык панели, как у
    /// Tooltip экрана.
    /// </summary>
    /// <param name="field">Элемент поля.</param>
    /// <param name="language">LCID выбранного языка или <c>null</c>.</param>
    /// <returns>Текст; подсказки нет или нет строки на языке — пусто.</returns>
    public string Text(VarReference field, int? language)
    {
        if (!_textIndexes.TryGetValue(field, out var index) || _strings.Languages is [])
        {
            return "";
        }
        return _strings.Text(LanguageChoice.Kept(_strings.Languages, language) ?? _strings.Languages[0], index);
    }
}
