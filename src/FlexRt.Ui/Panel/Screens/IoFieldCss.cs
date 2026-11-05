using System.Globalization;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>CSS I/O field на холсте: фон, рамка и углы, шрифт и выравнивание текста — из стилей файла.</summary>
public static class IoFieldCss
{
    /// <summary>Код стиля рамки Double line (<see cref="BorderStyle.EdgeStyle"/>).</summary>
    private const int DoubleLine = 2;

    /// <summary>Код выравнивания (<see cref="TextStyle"/>): по горизонтали Right, по вертикали Top.</summary>
    private const int AlignStart = 0;

    /// <summary>Код выравнивания: по горизонтали Left, по вертикали Bottom.</summary>
    private const int AlignEnd = 2;

    /// <summary>
    /// Фон, рамка и углы поля. Solid line — рамка цветом Color во всю ширину. Double line — как на снимке TIA
    /// пользователя (ширина 4, по пикселям): 1 px цветом Color снаружи, внутри — Background color, у края фона ещё 1 px
    /// цветом Color; другие ширины по снимкам не сверены. Внутренние линии — тенями внутрь, поэтому текст сдвинут
    /// переменной <c>--io-inner</c> на их ширину; ширина рамки CSS — в <c>--io-border</c> (маркеры выбора — от её края).
    /// </summary>
    /// <returns>Текст CSS.</returns>
    public static string Box(IoField field)
    {
        var b = field.Border;
        var css = $"background:{CssColor.Hex(field.Fill.ColorRgb)}; border-radius:{b.CornerRadius}px;";
        var border = BorderWidth(field);
        if (border == 0)
        {
            return css;
        }
        var color = CssColor.Hex(b.ColorRgb);
        if (border == b.Width)
        {
            return $"{css} border:{border}px solid {color}; --io-border:{border}px;";
        }
        var inner = b.Width - 1;
        return $"{css} border:{border}px solid {color}; --io-border:{border}px; --io-inner:{inner}px;"
            + $" box-shadow:inset 0 0 0 {inner - 1}px {CssColor.Hex(b.BackColorRgb)}, inset 0 0 0 {inner}px {color};";
    }

    /// <summary>
    /// Ширина рамки CSS поля: нет рамки — 0, Double line шире 1 — 1 (внутренние линии рисуются тенями), иначе ширина из
    /// файла. На неё сдвинуты координаты щелчка: <c>offsetX</c> браузер считает от внутреннего края рамки.
    /// </summary>
    /// <returns>Ширина в пикселях экрана.</returns>
    public static int BorderWidth(IoField field)
    {
        var b = field.Border;
        if (b.Width <= 0)
        {
            return 0;
        }
        return b.EdgeStyle == DoubleLine ? 1 : b.Width;
    }

    /// <summary>
    /// Текст поля: цвет, шрифт (размер TIA — в пикселях экрана; гипотеза по снимку TIA), начертание и выравнивание по
    /// горизонтали и вертикали.
    /// </summary>
    /// <returns>Текст CSS.</returns>
    public static string Text(IoField field)
    {
        var f = field.Font;
        var size = f.Size.ToString(CultureInfo.InvariantCulture);
        return $"color:{CssColor.Hex(field.Text.ColorRgb)}; font-family:'{FontName(f.Name)}'; font-size:{size}px;"
            + $" font-weight:{(f.Bold ? "bold" : "normal")}; font-style:{(f.Italic ? "italic" : "normal")};"
            + $" text-decoration:{(f.Underline ? "underline" : "none")};"
            + $" justify-content:{Flex(field.Text.HorizontalAlignment, "flex-end", "flex-start")};"
            + $" align-items:{Flex(field.Text.VerticalAlignment, "flex-start", "flex-end")};";
    }

    /// <summary>
    /// Имя шрифта из файла для строки CSS в одинарных кавычках: обратная косая черта и кавычка экранируются, переносы
    /// строки (в строке CSS недопустимы) заменяются пробелом — иначе испорченное имя сломало бы весь стиль текста.
    /// </summary>
    /// <returns>Имя, безопасное внутри <c>'…'</c>.</returns>
    private static string FontName(string name) =>
        name.Replace(@"\", @"\\").Replace("'", @"\'").Replace('\r', ' ').Replace('\n', ' ');

    /// <summary>Выравнивание flex по коду: 1 — по центру, код <c>0</c> — <paramref name="zero"/>, 2 — <paramref name="two"/>.</summary>
    /// <returns>Значение CSS; код не известен — по центру.</returns>
    private static string Flex(int code, string zero, string two) => code switch
    {
        AlignStart => zero,
        AlignEnd => two,
        _ => "center"
    };
}
