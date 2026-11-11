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
/// целого; что TIA округляет так же, сверено только на этом снимке. Начало строки задаёт выравнивание (по центру
/// может прийтись на полпикселя).
/// </summary>
public static class IoFieldGlyphs
{
    /// <summary>Шрифты, уже найденные в Windows: ключ — имя, жирность, курсив; не найден — <c>null</c>.</summary>
    private static readonly ConcurrentDictionary<(string Name, bool Bold, bool Italic), GlyphTypeface?> Fonts = new();

    /// <summary>Раскладка, уже посчитанная для поля: холст перерисовывается при каждом выборе объекта.</summary>
    private static readonly ConditionalWeakTable<IoField, IReadOnlyList<(char Glyph, int Width)>?[]> Layouts = new();

    /// <summary>Символы образца текста поля (<see cref="IoFieldTexts.Sample"/>) с шириной каждого в пикселях экрана.</summary>
    /// <param name="field">Поле: образец, шрифт и размер.</param>
    /// <returns>
    /// Символы и ширины; шрифта нет в Windows, имя — список шрифтов (<c>,</c> или <c>#</c>: CSS понял бы его иначе) или в
    /// шрифте нет символа — <c>null</c> (текст рисуется целиком).
    /// </returns>
    public static IReadOnlyList<(char Glyph, int Width)>? Layout(IoField field) =>
        Layouts.GetValue(field, f => [Measure(f)])[0];

    /// <summary>Посчитать ширины символов образца поля.</summary>
    /// <returns>Символы и ширины или <c>null</c> (см. <see cref="Layout"/>).</returns>
    private static IReadOnlyList<(char Glyph, int Width)>? Measure(IoField field)
    {
        var f = field.Font;
        if (f.Name.IndexOfAny([',', '#']) >= 0 || Font(f.Name, f.Bold, f.Italic) is not { } glyphs)
        {
            return null;
        }
        var text = IoFieldTexts.Sample(field);
        var result = new List<(char, int)>(text.Length);
        foreach (var c in text)
        {
            if (!glyphs.CharacterToGlyphMap.TryGetValue(c, out var index))
            {
                return null;
            }
            result.Add((c, (int)Math.Round(glyphs.AdvanceWidths[index] * f.Size, MidpointRounding.AwayFromZero)));
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
