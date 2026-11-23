using System.Globalization;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>Заливка, исходная рамка и текст кнопки из прочитанных стилей FWC.</summary>
public static class ButtonCss
{
    /// <summary>Заливка кнопки в пикселях или образец с видимыми крайними цветами.</summary>
    /// <param name="state">Оформление кнопки.</param>
    /// <param name="preview">Распределить цвета по образцу независимо от ширин переходов.</param>
    /// <returns>CSS background для холста или образца в инспекторе.</returns>
    public static string Background(ButtonStateAppearance state, bool preview = false)
    {
        if (state.Fill is not { } fill || fill.Transparent)
        {
            return "background:transparent;";
        }
        var middle = CssColor.Hex(fill.ColorRgb);
        var first = state.FirstGradient is { Width: > 0 } upper ? upper : null;
        var second = state.SecondGradient is { Width: > 0 } lower ? lower : null;
        if (first is null && second is null)
        {
            return $"background:{middle};";
        }
        var direction = (first ?? second)!.Vertical ? "to bottom" : "to right";
        var start = first is null ? middle : CssColor.Hex(first.ColorRgb);
        var end = second is null ? middle : CssColor.Hex(second.ColorRgb);
        if (preview)
        {
            return $"background:linear-gradient({direction},{start} 0% 12%,{middle} 50%,{end} 88% 100%);";
        }
        return $"background:linear-gradient({direction},{start} 0px,{middle} {first?.Width ?? 0}px,"
            + $"{middle} calc(100% - {second?.Width ?? 0}px),{end} 100%);";
    }

    /// <summary>Основа кнопки: заливка и обрезка по прочитанному радиусу углов.</summary>
    /// <returns>CSS фона и углов.</returns>
    public static string Box(ButtonStateAppearance state) => Background(state)
        + (state.Border is { } border ? $"border-radius:{border.CornerRadius}px;" : "");

    /// <summary>Исходная растягиваемая картинка рамки: углы не масштабируются, стороны и центр растягиваются.</summary>
    /// <returns>CSS девятичастного растягивания TILED_PICTURE.</returns>
    public static string Skin(ButtonFrame frame, string url) =>
        $"border-image-source:url('{url}'); border-image-slice:{frame.Top} {frame.Right} {frame.Bottom} {frame.Left} fill;"
        + $" border-width:{frame.Top}px {frame.Right}px {frame.Bottom}px {frame.Left}px;"
        + $" border-image-width:{frame.Top}px {frame.Right}px {frame.Bottom}px {frame.Left}px;";

    /// <summary>Область графической подписи внутри рамки: поля и выравнивание из PS_Position.</summary>
    /// <returns>CSS положения и flex-выравнивания.</returns>
    public static string PictureArea(ButtonStateAppearance state)
    {
        var border = state.Border?.Width ?? 0;
        var margins = state.Picture?.Margins;
        var top = border + (margins?.Top ?? 0);
        var right = border + (margins?.Right ?? 0);
        var bottom = border + (margins?.Bottom ?? 0);
        var left = border + (margins?.Left ?? 0);
        var horizontal = state.Picture?.HorizontalAlignment switch { 0 => "flex-end", 2 => "flex-start", _ => "center" };
        var vertical = state.Picture?.VerticalAlignment switch { 0 => "flex-start", 2 => "flex-end", _ => "center" };
        return $"inset:{top}px {right}px {bottom}px {left}px;justify-content:{horizontal};align-items:{vertical};";
    }

    /// <summary>Размер графической подписи: растянуть по области или оставить исходные пиксели.</summary>
    /// <returns>CSS размера картинки.</returns>
    public static string Picture(bool? stretch) => stretch == false
        ? "width:auto;height:auto;max-width:none;max-height:none;object-fit:none;"
        : "width:100%;height:100%;max-width:none;max-height:none;object-fit:fill;";

    /// <summary>Рамка без картинки: рисуются только подтверждённые Solid и Double line.</summary>
    /// <returns>CSS внутренних линий; стиль пунктиров без исходной картинки не угадывается.</returns>
    public static string Border(BorderStyle? border)
    {
        if (border is null || border.Width <= 0)
        {
            return "";
        }
        var color = CssColor.Hex(border.ColorRgb);
        if (border is { EdgeStyle: 1 } or { EdgeStyle: 2, Width: 1 })
        {
            return $"box-shadow:inset 0 0 0 {border.Width}px {color};";
        }
        return border.EdgeStyle == 2
            ? $"box-shadow:inset 0 0 0 1px {color},inset 0 0 0 {border.Width - 1}px {CssColor.Hex(border.BackColorRgb)},inset 0 0 0 {border.Width}px {color};"
            : "";
    }

    /// <summary>Текст внутри рамки: цвет, шрифт, отступы, выравнивание и угол из PS_Text.</summary>
    /// <returns>CSS области текста кнопки.</returns>
    public static string Text(ButtonStateAppearance state, ScreenRect bounds, PanelFont? font, int? width)
    {
        var border = state.Border?.Width ?? 0;
        var css = $"inset:{border}px;";
        var fontCss = font is null ? "" : Font(font);
        if (state.Text is not { } text)
        {
            return css + fontCss;
        }
        css += $"color:{CssColor.Hex(text.ColorRgb)};";
        css += text.Orientation is 90 or 270
            ? VerticalText(text, bounds, border, width)
            : HorizontalText(text, bounds, border, width);
        return css + fontCss;
    }

    /// <summary>Выравнивание, отступы и центрирование горизонтальной подписи.</summary>
    /// <returns>CSS положения текста.</returns>
    private static string HorizontalText(TextStyle text, ScreenRect bounds, int border, int? width)
    {
        var css = $" justify-content:{Horizontal(text.HorizontalAlignment)};"
            + $" align-items:{Vertical(text.VerticalAlignment)};";
        var margins = text.Margins;
        if (margins is not null)
        {
            css += $"padding:{margins.Top}px {margins.Right}px {margins.Bottom}px {margins.Left}px;";
        }
        var available = bounds.Width - 2 * border - (margins?.Left ?? 0) - (margins?.Right ?? 0);
        return css + Snap(text.HorizontalAlignment, available, width);
    }

    /// <summary>Повернуть область подписи и перевести отступы и выравнивание в её координаты.</summary>
    /// <returns>CSS поворота, положения и обрезки текста внутри рамки.</returns>
    private static string VerticalText(TextStyle text, ScreenRect bounds, int border, int? width)
    {
        var leftRotation = text.Orientation == 270;
        var boxWidth = Math.Max(0, bounds.Height - 2 * border);
        var boxHeight = Math.Max(0, bounds.Width - 2 * border);
        var horizontal = leftRotation ? Horizontal(text.VerticalAlignment) : Vertical(text.VerticalAlignment);
        var vertical = leftRotation ? Horizontal(text.HorizontalAlignment) : Vertical(text.HorizontalAlignment);
        var css = $" inset:auto;left:50%;top:50%;width:{boxWidth}px;height:{boxHeight}px;"
            + $" transform:translate(-50%,-50%) rotate({text.Orientation}deg);"
            + $" justify-content:{horizontal};align-items:{vertical};";
        var margins = text.Margins;
        if (margins is not null)
        {
            var edges = leftRotation
                ? $"{margins.Left}px {margins.Top}px {margins.Right}px {margins.Bottom}px"
                : $"{margins.Right}px {margins.Bottom}px {margins.Left}px {margins.Top}px";
            css += $" padding:{edges};clip-path:inset({edges});";
        }
        var available = boxWidth - (margins?.Top ?? 0) - (margins?.Bottom ?? 0);
        return css + Snap(text.VerticalAlignment, available, width);
    }

    /// <summary>Шрифт подписи на выбранном языке; размер TIA измеряется в пикселях панели.</summary>
    /// <returns>Безопасная строка CSS шрифта.</returns>
    private static string Font(PanelFont font)
    {
        var name = font.Name.Replace(@"\", @"\\").Replace("'", @"\'").Replace('\r', ' ').Replace('\n', ' ');
        var decorations = new List<string>();
        if (font.Underline)
        {
            decorations.Add("underline");
        }
        if (font.Strikeout)
        {
            decorations.Add("line-through");
        }
        return $"font-family:'{name}';font-size:{font.Size.ToString(CultureInfo.InvariantCulture)}px;"
            + $"font-weight:{(font.Bold ? "bold" : "normal")};font-style:{(font.Italic ? "italic" : "normal")};"
            + $"text-decoration:{(decorations.Count == 0 ? "none" : string.Join(' ', decorations))};"
            + (ScreenTextGlyphs.LineHeight(font) is { } height ? $"line-height:{height}px;" : "");
    }

    /// <summary>Сдвиг центрированной подписи вдоль строки к целому пикселю.</summary>
    /// <returns>CSS сдвига глифов; измеренной ширины нет — пусто.</returns>
    private static string Snap(int alignment, int available, int? width)
    {
        if (alignment != 1 || width is not { } measured)
        {
            return "";
        }
        return (available - measured) % 2 == 0 ? "" : $"--button-snap-x:{(available < measured ? "0.5" : "-0.5")}px;";
    }

    /// <summary>Выравнивание по горизонтали в кодах PS_Text.</summary>
    /// <returns>Значение CSS justify-content.</returns>
    private static string Horizontal(int code) => code switch { 0 => "flex-end", 2 => "flex-start", _ => "center" };

    /// <summary>Выравнивание по вертикали в кодах PS_Text.</summary>
    /// <returns>Значение CSS align-items.</returns>
    private static string Vertical(int code) => code switch { 0 => "flex-start", 2 => "flex-end", _ => "center" };
}
