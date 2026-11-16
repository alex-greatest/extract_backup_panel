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

    /// <summary>Отступы текста при непрочитанных Margins: по 2 px слева и справа.</summary>
    private static readonly TextMargins DefaultMargins = new(0, 0, 2, 2);

    /// <summary>
    /// Фон, рамка и углы поля. Solid line — рамка цветом Color во всю ширину. Double line — как на снимке TIA
    /// редактора TIA V21 (ширины 1–6): 1 px цветом Color снаружи, внутри — Background color, у края фона ещё 1 px
    /// цветом Color при ширине от 2. Внутренние линии — тенями внутрь, поэтому текст сдвинут
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
    /// Текст поля: цвет, шрифт (размер TIA — в пикселях экрана; сверено на Tahoma 13/16/23/25), начертание, выравнивание по
    /// горизонтали и вертикали и Margins — отступы от внутреннего края рамки (сверено со снимком редактора TIA на шаге 80:
    /// справа 2 px до текста, как Right). Margins не прочитаны — отступы из CSS (2 px слева и справа).
    /// Шрифт передаётся для выбранного языка. Обрезка — по Margins, включая частичные символы. Углы 90/270 поворачивают
    /// текст и область обрезки внутри рамки; Strikeout добавляет зачёркивание.
    /// </summary>
    /// <returns>Текст CSS.</returns>
    public static string Text(IoField field, ScreenRect bounds, PanelFont font, int? textWidth = null, int? textHeight = null)
    {
        var size = font.Size.ToString(CultureInfo.InvariantCulture);
        return $"color:{CssColor.Hex(field.Text.ColorRgb)}; font-family:'{FontName(font.Name)}'; font-size:{size}px;"
            + $" font-weight:{(font.Bold ? "bold" : "normal")}; font-style:{(font.Italic ? "italic" : "normal")};"
            + $" text-decoration:{Decoration(font)};"
            + (textHeight is { } height ? $" line-height:{height}px;" : "")
            + Alignment(field.Text)
            + Padding(field.Text)
            + PixelAlignment(field, bounds, textWidth, textHeight)
            + Rotation(field, bounds);
    }

    /// <summary>Сохранить физические направления выравнивания при повороте внутренней области текста.</summary>
    /// <returns>CSS выравнивания вдоль повёрнутых осей.</returns>
    private static string Alignment(TextStyle text)
    {
        var horizontal = Flex(text.HorizontalAlignment, "flex-end", "flex-start");
        var vertical = Flex(text.VerticalAlignment, "flex-start", "flex-end");
        return text.Orientation switch
        {
            270 => $" justify-content:{Flex(text.VerticalAlignment, "flex-end", "flex-start")}; align-items:{horizontal};",
            90 => $" justify-content:{vertical}; align-items:{Flex(text.HorizontalAlignment, "flex-start", "flex-end")};",
            _ => $" justify-content:{horizontal}; align-items:{vertical};"
        };
    }

    /// <summary>Поля от физических краёв объекта переводятся в координаты повёрнутого текста.</summary>
    /// <returns>CSS отступов; поля не прочитаны — отступы по умолчанию.</returns>
    private static string Padding(TextStyle text)
    {
        var m = text.Margins ?? DefaultMargins;
        var edges = text.Orientation switch
        {
            270 => $"{m.Left}px {m.Top}px {m.Right}px {m.Bottom}px",
            90 => $"{m.Right}px {m.Bottom}px {m.Left}px {m.Top}px",
            _ => $"{m.Top}px {m.Right}px {m.Bottom}px {m.Left}px"
        };
        return $" padding:{edges}; clip-path:inset({edges});";
    }

    /// <summary>Центр строки в целых пикселях: половина свободной ширины с округлением к нулю, как у редактора TIA.</summary>
    /// <returns>Сдвиг глифов на полпикселя, если flex поместил бы их между пикселями.</returns>
    private static string PixelAlignment(IoField field, ScreenRect bounds, int? textWidth, int? textHeight)
    {
        var text = field.Text;
        var margins = text.Margins ?? DefaultMargins;
        var width = Math.Max(0, bounds.Width - 2 * field.Border.Width - margins.Left - margins.Right);
        var height = Math.Max(0, bounds.Height - 2 * field.Border.Width - margins.Top - margins.Bottom);
        var rotated = text.Orientation is 90 or 270;
        var x = PixelOffset(rotated ? height : width, textWidth, rotated ? text.VerticalAlignment : text.HorizontalAlignment);
        var y = PixelOffset(rotated ? width : height, textHeight, rotated ? text.HorizontalAlignment : text.VerticalAlignment);
        return $" --io-snap-x:{x}px; --io-snap-y:{y}px;";
    }

    /// <summary>Убрать дробную половину свободного места при центрировании вдоль одной оси.</summary>
    /// <returns>Сдвиг к целому пикселю; другие выравнивания не сдвигаются.</returns>
    private static string PixelOffset(int available, int? length, int alignment)
    {
        if (alignment != 1 || length is not { } size || (available - size) % 2 == 0)
        {
            return "0";
        }
        return available < size ? "0.5" : "-0.5";
    }

    /// <summary>Подчёркивание и зачёркивание могут быть включены одновременно.</summary>
    /// <returns>Значение text-decoration.</returns>
    private static string Decoration(PanelFont font) => (font.Underline, font.Strikeout) switch
    {
        (true, true) => "underline line-through",
        (true, false) => "underline",
        (false, true) => "line-through",
        _ => "none"
    };

    /// <summary>Повернуть внутреннюю область текста на подтверждённый угол PS_Text, поменяв её ширину и высоту.</summary>
    /// <returns>CSS поворота; горизонтальный или неизвестный угол — без поворота.</returns>
    private static string Rotation(IoField field, ScreenRect bounds)
    {
        if (field.Text.Orientation is not (90 or 270))
        {
            return "";
        }
        var width = Math.Max(0, bounds.Height - 2 * field.Border.Width);
        var height = Math.Max(0, bounds.Width - 2 * field.Border.Width);
        return $" inset:auto; left:50%; top:50%; width:{width}px; height:{height}px;"
            + $" transform:translate(-50%,-50%) rotate({field.Text.Orientation}deg);";
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
