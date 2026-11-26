using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel.Screens.Styles;

namespace FlexRt.Parsing.Panel.Screens.Buttons.Appearance;

/// <summary>Оформление кнопки FwdButtonLean: Normal, Pressed и Focus; подтверждено на TP700 Comfort, TIA V21.</summary>
internal static class ButtonAppearanceReader
{
    /// <summary>Смещение размера FwdButtonLean от конца ссылок объекта.</summary>
    private const int SizeOffset = 0x14;

    /// <summary>Начало FwdButtonLean от конца ссылок объекта.</summary>
    private const int DataOffset = 0x18;

    /// <summary>Подтверждённая длина FwdButtonLean.</summary>
    private const int DataSize = 0x3d;

    /// <summary>Версия FwdButtonLean в проверенных скомпилированных кнопках.</summary>
    private const long StreamVersion = 0x8000000a;

    /// <summary>Начало четырёх ссылок Normal от конца ссылок объекта.</summary>
    private const int NormalOffset = 0x31;

    /// <summary>Начало четырёх ссылок Pressed от конца ссылок объекта.</summary>
    private const int PressedOffset = 0x41;

    /// <summary>Ссылка PS_Focus от конца ссылок объекта.</summary>
    private const int FocusOffset = 0x51;

    /// <summary>Метка конца подписей; за ней ссылка на FONT в порядке InstanceId, ClassId.</summary>
    private const long CaptionEnd = 0x9ffffe00;

    /// <summary>Прочитать подтверждённую форму; неизвестная форма не интерпретируется, сбой компонента сохраняет остальные.</summary>
    /// <returns>Оформление или null для неподтверждённой формы.</returns>
    public static ButtonAppearance? Read(FwxDocument doc, TocEntry table, TableItem item, long body)
    {
        var b = doc.Binary;
        if (b.D4(item.Offset + body + SizeOffset) != DataSize || b.D4(item.Offset + body + DataOffset) != StreamVersion)
        {
            return null;
        }
        return new ButtonAppearance(
            ReadState(doc, table, item, body + NormalOffset),
            ReadState(doc, table, item, body + PressedOffset),
            ButtonDataReader.ReadValue(doc, () => ReadFonts(doc, table, item, body), []))
        {
            Focus = ReadStyle(doc, table, item, body + FocusOffset, ButtonFocusReader.Table, ButtonFocusReader.Read)
        };
    }

    /// <summary>Прочитать рамку, текст и заливку одного состояния независимо друг от друга.</summary>
    /// <returns>Оформление состояния; непрочитанные компоненты — null.</returns>
    private static ButtonStateAppearance ReadState(FwxDocument doc, TocEntry table, TableItem item, long offset)
    {
        var borderReference = ButtonDataReader.ReadValue(doc,
            () => ButtonDataReader.StyleReference(doc, table, item, offset, BorderStyleReader.Table), null);
        var border = borderReference is { } link ? ButtonAppearanceStyleReader.ReadBorder(doc, link) : default;
        var text = ReadStyle(doc, table, item, offset + 4, TextStyleReader.Table, TextStyleReader.Read);
        var fill = ReadStyle(doc, table, item, offset + 8, FillStyleReader.Table, ButtonAppearanceStyleReader.ReadFill);
        var picture = ReadStyle(doc, table, item, offset + 0x0c, ButtonPictureLayoutReader.Table, ButtonPictureLayoutReader.Read);
        return new ButtonStateAppearance(border.Border, text, fill, border.First, border.Second, border.Frame, picture);
    }

    /// <summary>Прочитать один стиль; ошибка ссылки или записи не удаляет остальные свойства кнопки.</summary>
    /// <returns>Компонент оформления или null.</returns>
    private static T? ReadStyle<T>(FwxDocument doc, TocEntry table, TableItem item, long offset, string name,
        Func<FwxDocument, TocEntry, int, string, T> read) where T : class
    {
        return ButtonDataReader.ReadValue(doc, () =>
        {
            var reference = ButtonDataReader.StyleReference(doc, table, item, offset, name);
            return reference is { } link ? read(doc, doc.FindTable(link.Table)!, link.Index, "кнопка") : null;
        }, null);
    }

    /// <summary>Найти конец проверенных подписей и прочитать шрифты по следующей ссылке FONT.</summary>
    /// <returns>Шрифты по языкам Runtime; ссылки нет — пустой список.</returns>
    /// <exception cref="FwxFormatException">Метка, ссылка или запись FONT повреждены.</exception>
    private static IReadOnlyList<LanguageFont> ReadFonts(FwxDocument doc, TocEntry table, TableItem item, long body)
    {
        var cursor = body + (DataOffset + DataSize + 3) / 4 * 4;
        while (true)
        {
            ButtonDataReader.Check(item, cursor, 4, table.Name);
            if (doc.Binary.D4(item.Offset + cursor) == CaptionEnd)
            {
                break;
            }
            cursor += 8;
        }
        var reference = ButtonDataReader.StyleReference(doc, table, item, cursor + 4, FontReader.Table, true);
        return reference is { } link ? FontReader.Read(doc, doc.FindTable(link.Table)!, link.Index, "кнопка") : [];
    }
}
