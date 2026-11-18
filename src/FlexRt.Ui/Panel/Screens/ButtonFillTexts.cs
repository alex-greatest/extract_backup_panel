using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Режим заливки кнопки и границы восстановления исходных настроек из FWC.</summary>
public static class ButtonFillTexts
{
    /// <summary>Название заливки по прозрачности, направлениям и эффективным ширинам переходов.</summary>
    /// <returns>Режим или варианты, которые скомпилированные данные не различают.</returns>
    public static string Pattern(ButtonStateAppearance state)
    {
        if (state.Fill is null)
        {
            return "не прочитано";
        }
        if (state.Fill.Transparent)
        {
            return "Transparent";
        }
        if (state.FirstGradient is not { } first || state.SecondGradient is not { } second)
        {
            return "не прочитано";
        }
        return (first.Width, second.Width) switch
        {
            (0, 0) => (first.Vertical, second.Vertical) switch
            {
                (true, true) => "Vertical gradient",
                (false, false) => "Solid / Horizontal gradient",
                _ => "не определяется однозначно"
            },
            (> 0, > 0) when first.Vertical != second.Vertical => "не определяется однозначно",
            _ => (first.Width > 0 ? first.Vertical : second.Vertical) ? "Vertical gradient" : "Horizontal gradient"
        };
    }

    /// <summary>Присутствует ли переход ненулевой ширины у непрозрачной заливки.</summary>
    /// <returns>Признак эффективного градиента.</returns>
    public static bool HasGradient(ButtonStateAppearance state) => state.Fill is { Transparent: false }
        && (state.FirstGradient is { Width: > 0 } || state.SecondGradient is { Width: > 0 });

    /// <summary>Состояние перехода без подмены нулевой ширины выключенной галочкой.</summary>
    /// <returns>Подтверждённое включение, неоднозначность или отсутствие прочитанных данных.</returns>
    public static string Enabled(ButtonGradient? gradient) => gradient switch
    {
        null => "не прочитано",
        { Width: > 0 } => "включён",
        _ => "выключен или Width = 0"
    };
}
