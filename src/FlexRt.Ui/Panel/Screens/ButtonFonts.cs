using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Выбор шрифта кнопки для холста и свойств на текущем языке.</summary>
public static class ButtonFonts
{
    /// <summary>Выбрать шрифт языка или первую доступную запись FONT.</summary>
    /// <returns>Шрифт подписи; записи FONT отсутствуют — null.</returns>
    public static PanelFont? Of(ButtonGeneral button, int? language)
    {
        if (button.Appearance?.Fonts is not { Count: > 0 } fonts)
        {
            return null;
        }
        return fonts.FirstOrDefault(font => font.Language == language)?.Font ?? fonts[0].Font;
    }
}
