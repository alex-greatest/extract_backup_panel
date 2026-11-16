using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using FlexRt.Export.ScreenObjects;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>
/// Раскладка текста I/O field по символам с шириной каждого в целых пикселях, как рисует редактор TIA: у него каждая
/// буква занимает целое число пикселей (Tahoma Bold 13 px: цифра — 8 px, в браузере — 8,28), и на шаге 80 строка
/// <c>+000000000</c> у TIA на 4 px уже, чем у браузера. Ширина — ширина глифа шрифта Windows (WPF), округлённая до
/// целого. Дополнительно сверены Tahoma Bold 16/23/25, узкие поля с тремя выравниваниями и поля A603.
/// Начало центрированной строки приводится к целым пикселям в <see cref="IoFieldCss"/>.
/// </summary>
public static class IoFieldGlyphs
{
    /// <summary>Шрифты, уже найденные в Windows: ключ — имя, жирность, курсив; не найден — <c>null</c>.</summary>
    private static readonly ConcurrentDictionary<(string Name, bool Bold, bool Italic), GlyphTypeface?> Fonts = new();

    /// <summary>Раскладки по полю, шрифту и языку образца Date/Time: холст перерисовывается при выборе объекта.</summary>
    private static readonly ConditionalWeakTable<IoField, Dictionary<(PanelFont Font, int? Language), IReadOnlyList<(char Glyph, int Width)>?>> Layouts = new();

    /// <summary>Высота строки шрифта Windows в целых пикселях; Tahoma 13/16/23/25 проверена с TIA при масштабе 100%.</summary>
    /// <returns>Высота; шрифт не найден — null.</returns>
    public static int? LineHeight(PanelFont font) => Font(font.Name, font.Bold, font.Italic) is { } glyphs
        ? (int)Math.Round(glyphs.Height * font.Size, MidpointRounding.AwayFromZero)
        : null;

    /// <summary>Символы образца текста поля (<see cref="IoFieldTexts.Sample"/>) с шириной каждого в пикселях экрана.</summary>
    /// <param name="field">Поле с образцом текста.</param>
    /// <param name="font">Шрифт выбранного языка и его размер.</param>
    /// <param name="language">Язык образца Date/Time; без выбора — первый язык поля.</param>
    /// <returns>
    /// Символы и ширины; шрифта нет в Windows, имя — список шрифтов (<c>,</c> или <c>#</c>: CSS понял бы его иначе) или в
    /// шрифте нет символа — <c>null</c> (текст рисуется целиком).
    /// </returns>
    public static IReadOnlyList<(char Glyph, int Width)>? Layout(IoField field, PanelFont font, int? language = null)
    {
        var layouts = Layouts.GetValue(field, _ => new Dictionary<(PanelFont, int?), IReadOnlyList<(char Glyph, int Width)>?>());
        var key = (font, language);
        if (layouts.TryGetValue(key, out var layout))
        {
            return layout;
        }
        layout = Measure(field, font, language);
        layouts.Add(key, layout);
        return layout;
    }

    /// <summary>Посчитать ширины символов образца поля.</summary>
    /// <returns>Символы и ширины или <c>null</c> (см. <see cref="Layout"/>).</returns>
    private static List<(char Glyph, int Width)>? Measure(IoField field, PanelFont font, int? language)
    {
        if (font.Name.IndexOfAny([',', '#']) >= 0 || Font(font.Name, font.Bold, font.Italic) is not { } glyphs)
        {
            return null;
        }
        var text = IoFieldTexts.Sample(field, language);
        var result = new List<(char, int)>(text.Length);
        foreach (var c in text)
        {
            if (!glyphs.CharacterToGlyphMap.TryGetValue(c, out var index))
            {
                return null;
            }
            result.Add((c, (int)Math.Round(glyphs.AdvanceWidths[index] * font.Size, MidpointRounding.AwayFromZero)));
        }
        return result;
    }

    /// <summary>Глифы шрифта Windows по имени и начертанию; найденный запоминается.</summary>
    /// <returns>Глифы; шрифта нет — <c>null</c>.</returns>
    private static GlyphTypeface? Font(string name, bool bold, bool italic) =>
        Fonts.GetOrAdd((name, bold, italic), k => new Typeface(
                new FontFamily(k.Name),
                k.Italic ? FontStyles.Italic : FontStyles.Normal,
                k.Bold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal)
            .TryGetGlyphTypeface(out var found) ? found : null);
}
