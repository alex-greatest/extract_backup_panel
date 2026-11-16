using System.Globalization;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Export.ScreenObjects;

/// <summary>Фиксированный образец Date/Time редактора TIA V21, проверенный на английском, немецком и русском.</summary>
internal static class IoFieldDateTimeSample
{
    /// <summary>Код формата Date в элементе I/O field.</summary>
    private const int DateFormat = 4;

    /// <summary>Код формата Time в элементе I/O field.</summary>
    private const int TimeFormat = 5;

    /// <summary>Код формата Date/time в элементе I/O field.</summary>
    private const int DateTimeFormat = 6;

    /// <summary>Язык исходного образца при отсутствии языковых записей FONT.</summary>
    private const int English = 0x409;

    /// <summary>Дата и время образца редактора; значение тега и часы компьютера не используются.</summary>
    private static readonly DateTime Value = new(2002, 12, 31, 10, 59, 59);

    /// <summary>Образец в формате выбранного языка; без выбора — первого языка шрифта, без языков — английский.</summary>
    /// <returns>Дата, время или оба через пробел; неизвестный LCID — пусто.</returns>
    public static string Of(IoField field, int? language)
    {
        CultureInfo culture;
        try
        {
            var firstLanguage = field.Fonts.Count > 0 ? field.Fonts[0].Language : English;
            culture = CultureInfo.GetCultureInfo(language ?? firstLanguage);
        }
        catch (CultureNotFoundException)
        {
            return "";
        }
        return field.DisplayFormat switch
        {
            DateFormat => Value.ToString("d", culture),
            TimeFormat => Value.ToString("T", culture),
            DateTimeFormat => $"{Value.ToString("d", culture)} {Value.ToString("T", culture)}",
            _ => ""
        };
    }
}
