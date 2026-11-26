using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel.Screens.Styles;

namespace FlexRt.Parsing.Panel.Screens.Buttons.Appearance;

/// <summary>Расширенные стили кнопки: градиент, прозрачность и растягиваемая картинка оформления.</summary>
internal static class ButtonAppearanceStyleReader
{
    /// <summary>Подтверждённый размер PS_BorderedShap с градиентами и ссылкой оформления кнопки.</summary>
    private const int ExtendedStyleSize = 0x50;

    /// <summary>Ссылка PS_BorderedShap на TILED_PICTURE в подтверждённой записи рамки с градиентом.</summary>
    private const int FrameOffset = 0x2c;

    /// <summary>Начало первого перехода: направление, ширина, служебное слово и цвет.</summary>
    private const int FirstGradientOffset = 0x34;

    /// <summary>Начало второго перехода той же раскладки.</summary>
    private const int SecondGradientOffset = 0x44;

    /// <summary>Ширина, высота и четыре границы растягивания в TILED_PICTURE.</summary>
    private const int FrameMetricsOffset = 0x10;

    /// <summary>Число байтов размеров и границ растягивания.</summary>
    private const int FrameMetricsSize = 0x0c;

    /// <summary>Цвет заливки PS_Fill_Color в формате R, G, B, признак прозрачности.</summary>
    private const int FillOffset = 0x14;

    /// <summary>Прочитать общую рамку; градиенты и картинку — только из подтверждённой записи длиной 0x50.</summary>
    /// <returns>Рамка, два перехода и картинка; неизвестная форма расширений — null, ошибка компонента — null с предупреждением.</returns>
    public static (BorderStyle? Border, ButtonGradient? First, ButtonGradient? Second, ButtonFrame? Frame) ReadBorder(
        FwxDocument doc, VarReference reference)
    {
        var table = doc.FindTable(reference.Table)!;
        var item = ElementLink.Item(doc, table, reference.Index, "оформление кнопки");
        var border = ButtonDataReader.ReadValue(doc,
            () => BorderStyleReader.Read(doc, table, reference.Index, "кнопка"), null);
        if (item.Length != ExtendedStyleSize)
        {
            return (border, null, null, null);
        }
        return (
            border,
            ButtonDataReader.ReadValue(doc, () => ReadGradient(doc, table, item, FirstGradientOffset), null),
            ButtonDataReader.ReadValue(doc, () => ReadGradient(doc, table, item, SecondGradientOffset), null),
            ButtonDataReader.ReadValue(doc, () => ReadFrame(doc, table, item), null));
    }

    /// <summary>Прочитать скомпилированный переход; ширина 0 не различает выключение и нулевой отступ редактора.</summary>
    /// <returns>Направление, ширина и крайний цвет перехода.</returns>
    /// <exception cref="FwxFormatException">Переход не помещается в запись или направление неизвестно.</exception>
    private static ButtonGradient ReadGradient(FwxDocument doc, TocEntry table, TableItem item, int offset)
    {
        ButtonDataReader.Check(item, offset, 0x0c, table.Name);
        var p = item.Offset + offset;
        var direction = doc.Binary.D2(p);
        return direction is 0 or 1
            ? new ButtonGradient(direction == 1, doc.Binary.D2(p + 2), StyleElement.Rgb(doc.Binary, p + 8))
            : throw new FwxFormatException(table.Name, p, $"направление градиента кнопки 0x{direction:X} не разобрано");
    }

    /// <summary>Прочитать ссылку картинки рамки и её размеры; порядок границ — Left, Top, Right, Bottom.</summary>
    /// <returns>Параметры TILED_PICTURE; ссылки нет — null.</returns>
    /// <exception cref="FwxFormatException">Ссылка или заголовок TILED_PICTURE повреждены.</exception>
    private static ButtonFrame? ReadFrame(FwxDocument doc, TocEntry table, TableItem item)
    {
        var reference = ButtonDataReader.StyleReference(doc, table, item, FrameOffset, "TILED_PICTURE");
        if (reference is not { } link)
        {
            return null;
        }
        var target = doc.FindTable(link.Table)!;
        var picture = StyleElement.Item(doc, target, link.Index, FrameMetricsOffset + FrameMetricsSize, "рамка кнопки");
        var p = picture.Offset + FrameMetricsOffset;
        var b = doc.Binary;
        return new ButtonFrame(link.Index, b.D2(p), b.D2(p + 2), b.D2(p + 4), b.D2(p + 6), b.D2(p + 8), b.D2(p + 0x0a));
    }

    /// <summary>Прочитать цвет и прозрачность заливки; градиент использует здесь средний цвет.</summary>
    /// <returns>Основной цвет и прозрачность.</returns>
    /// <exception cref="FwxFormatException">Элемент короткий или признак прозрачности не подтверждён.</exception>
    public static ButtonFill ReadFill(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, FillOffset + 4, owner);
        var p = item.Offset + FillOffset;
        var alpha = doc.Binary.D1(p + 3);
        return alpha is 0 or 0xff
            ? new ButtonFill(StyleElement.Rgb(doc.Binary, p), alpha == 0xff)
            : throw new FwxFormatException(table.Name, p + 3, $"признак прозрачности заливки кнопки 0x{alpha:X} не разобран");
    }
}
