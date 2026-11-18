using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Целочисленные размеры глифов Windows для подписей объектов на холсте TIA.</summary>
public static class ScreenTextGlyphs
{
    /// <summary>Найденные шрифты Windows по имени и начертанию.</summary>
    private static readonly ConcurrentDictionary<(string Name, bool Bold, bool Italic), GlyphTypeface?> Fonts = new();

    /// <summary>Высота строки шрифта в целых пикселях панели.</summary>
    /// <returns>Высота; шрифт Windows не найден — null.</returns>
    public static int? LineHeight(PanelFont font) => Font(font) is { } glyphs
        ? (int)Math.Round(glyphs.Height * font.Size, MidpointRounding.AwayFromZero)
        : null;

    /// <summary>Измерить каждый символ подписи с округлением до целого пикселя, как у поля.</summary>
    /// <returns>Символы и ширины; неизвестный шрифт или символ — null.</returns>
    public static IReadOnlyList<(char Glyph, int Width)>? Measure(string text, PanelFont font)
    {
        if (font.Name.IndexOfAny([',', '#']) >= 0 || Font(font) is not { } glyphs || text.IndexOfAny(['\r', '\n']) >= 0)
        {
            return null;
        }
        var result = new List<(char, int)>(text.Length);
        foreach (var character in text)
        {
            if (!glyphs.CharacterToGlyphMap.TryGetValue(character, out var index))
            {
                return null;
            }
            result.Add((character, (int)Math.Round(glyphs.AdvanceWidths[index] * font.Size, MidpointRounding.AwayFromZero)));
        }
        return result;
    }

    /// <summary>Найти глифы шрифта Windows и запомнить результат.</summary>
    /// <returns>Глифы; шрифт отсутствует — null.</returns>
    private static GlyphTypeface? Font(PanelFont font) =>
        Fonts.GetOrAdd((font.Name, font.Bold, font.Italic), key => new Typeface(
                new FontFamily(key.Name),
                key.Italic ? FontStyles.Italic : FontStyles.Normal,
                key.Bold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal)
            .TryGetGlyphTypeface(out var found) ? found : null);
}
