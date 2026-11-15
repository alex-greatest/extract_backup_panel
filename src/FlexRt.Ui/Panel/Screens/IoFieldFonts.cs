using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Выбор шрифта I/O field для холста и свойств на текущем языке.</summary>
public static class IoFieldFonts
{
    /// <summary>Выбранный язык; все языки или отсутствующая запись — прежний первый шрифт поля.</summary>
    /// <returns>Шрифт с размером и начертанием для отображения.</returns>
    public static PanelFont Of(IoField field, int? language) =>
        field.Fonts.FirstOrDefault(font => font.Language == language)?.Font ?? field.Font;
}
