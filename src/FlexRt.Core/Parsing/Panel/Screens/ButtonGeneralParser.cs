using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>General кнопки: подтверждённые раскладки VIEW_BUTTONS и VIEW_STATEBUTTO, steps-buttons V21 TP700.</summary>
public static class ButtonGeneralParser
{
    /// <summary>Код вида на +0x10 от конца ссылок.</summary>
    private const int KindOffset = 0x10;

    /// <summary>Размер блока оформления на +0x14 от конца ссылок.</summary>
    private const int BlockSizeOffset = 0x14;

    /// <summary>Начало блока оформления на +0x18 от конца ссылок.</summary>
    private const int BlockOffset = 0x18;

    /// <summary>Дескрипторы подписей начинаются на границе четырёх байт после блока оформления.</summary>
    private const int CaptionAlignment = 4;

    /// <summary>Режим Mode на +0x1c от конца ссылок.</summary>
    private const int ModeOffset = 0x1c;

    /// <summary>Признак отдельной нажатой подписи на +0x24; в опытах 0 или 2.</summary>
    private const int PressedOffset = 0x24;

    /// <summary>PictureAutoSizing на +0x28: 0 — None, 1 — StretchPicture.</summary>
    private const int PictureAutoSizingOffset = 0x28;

    /// <summary>Обычная кнопка Text, Graphic или список.</summary>
    private const long ButtonKind = 0x54C000;

    /// <summary>Кнопка с отдельным нажатым состоянием.</summary>
    private const long StateKind = 0xEDC000;

    /// <summary>Невидимая кнопка с укороченным телом.</summary>
    private const long InvisibleKind = 0x628000;

    /// <summary>Прочитать General каждого элемента один раз; неизвестные виды пропускаются, ошибка не удаляет рамку объекта.</summary>
    public static void Parse(FwxDocument doc)
    {
        var objects = doc.ScreenObjects.Where(o => o.Table is "VIEW_BUTTONS" or "VIEW_STATEBUTTO").ToList();
        var directKeys = ButtonDirectKeyReader.Read(doc, objects);
        foreach (var obj in objects.DistinctBy(o => o.Element))
        {
            try
            {
                if (ReadRecord(doc, obj) is { } button)
                {
                    doc.Buttons.Add(directKeys.Contains(obj.Element)
                        ? button with { DirectKeys = directKeys[obj.Element].ToLookup(key => key.Screen, key => key.Call) }
                        : button);
                }
            }
            catch (FwxFormatException error)
            {
                doc.Warnings.Add(error.Message);
            }
        }
    }

    /// <summary>Прочитать режим и подписи; неподтверждённый код вида возвращает null.</summary>
    /// <returns>Свойства кнопки или null.</returns>
    /// <exception cref="FwxFormatException">Поля General или подписи повреждены.</exception>
    private static ButtonGeneral? ReadRecord(FwxDocument doc, ScreenObject obj)
    {
        var table = doc.FindTable(obj.Element.Table)!;
        var item = ElementLink.Item(doc, table, obj.Element.Index);
        var b = doc.Binary;
        var body = NamedElement.TryBodyOffset(b, item)
            ?? throw new FwxFormatException(table.Name, item.Offset, "заголовок кнопки не разобран");
        ButtonDataReader.Check(item, body, BlockOffset, table.Name);
        var kind = b.D4(item.Offset + body + KindOffset);
        if (kind is not (ButtonKind or StateKind or InvisibleKind))
        {
            return null;
        }
        var blockSize = b.D4(item.Offset + body + BlockSizeOffset);
        if (kind == InvisibleKind)
        {
            ButtonDataReader.Check(item, body + BlockOffset, blockSize, table.Name);
            var emptyCaption = new ButtonCaption(null, null);
            var invisible = new ButtonGeneral(obj.Element, ButtonMode.Invisible, null, emptyCaption, emptyCaption, null);
            return ReadOperatorProperties(doc, table, item, body, invisible);
        }
        ButtonDataReader.Check(item, body, PressedOffset + 4, table.Name);
        var code = b.D4(item.Offset + body + ModeOffset);
        if (code > (long)ButtonMode.GraphicAndText)
        {
            return null;
        }
        var mode = (ButtonMode)code;
        bool? pressed = b.D4(item.Offset + body + PressedOffset) switch { 0 => false, 2 => true, _ => null };
        var styleEnd = BlockOffset + blockSize;
        var captionOffset = body + (styleEnd + CaptionAlignment - 1) / CaptionAlignment * CaptionAlignment;
        var (off, on) = ButtonCaptionReader.Read(doc, item, captionOffset, table.Name);
        var button = new ButtonGeneral(obj.Element, mode, pressed, off, on, ReadList(doc, item, body, table.Name, mode))
        {
            Appearance = ButtonAppearanceReader.Read(doc, table, item, body),
            StretchPicture = ReadPictureStretch(doc, item, body, table.Name)
        };
        return ReadOperatorProperties(doc, table, item, body, button);
    }

    /// <summary>Прочитать Appearance, Flashing и Security из заголовка кнопки. Ошибка права сохраняет остальные свойства.</summary>
    /// <returns>Кнопка со считанными свойствами управления.</returns>
    private static ButtonGeneral ReadOperatorProperties(FwxDocument doc, TocEntry table, TableItem item, long body, ButtonGeneral button)
    {
        var links = NamedElement.TryReferences(doc.Binary, item) ?? [];
        var allowOperatorControl = ScreenObjectSecurityReader.AllowsOperatorControl(doc.Binary.D4(item.Offset + body));
        var flashing = ScreenObjectFlashingReader.ReadButton(doc, links);
        long? authorization = null;
        var authorizationRead = false;
        try
        {
            authorization = ScreenObjectSecurityReader.ReadAuthorization(doc, links);
            authorizationRead = true;
        }
        catch (FwxFormatException error)
        {
            doc.Warnings.Add(error.Message);
        }
        return button with
        {
            AllowOperatorControl = allowOperatorControl,
            Authorization = authorization,
            AuthorizationRead = authorizationRead,
            AnimatedAppearance = AppearanceAnimationReader.Read(doc, table, item, body, links),
            Events = ButtonEventReader.Read(doc, table, item, body, links),
            Flashing = flashing
        };
    }

    /// <summary>Прочитать PictureAutoSizing независимо от остальных свойств кнопки.</summary>
    /// <returns><c>true</c> для StretchPicture, <c>false</c> для None; ошибка или неизвестный код — null.</returns>
    private static bool? ReadPictureStretch(FwxDocument doc, TableItem item, long body, string section)
    {
        try
        {
            ButtonDataReader.Check(item, body, PictureAutoSizingOffset + 4, section);
            var offset = item.Offset + body + PictureAutoSizingOffset;
            return doc.Binary.D4(offset) switch
            {
                0 => false,
                1 => true,
                var code => throw new FwxFormatException(section, offset,
                    $"код PictureAutoSizing кнопки 0x{code:X} не разобран")
            };
        }
        catch (FwxFormatException error)
        {
            if (!doc.Warnings.Contains(error.Message))
            {
                doc.Warnings.Add(error.Message);
            }
            return null;
        }
    }

    /// <summary>Прочитать список отдельно: ошибка списка сохраняет режим и прочие свойства кнопки.</summary>
    /// <returns>Список; его нет — null, ошибка — Entries null с предупреждением.</returns>
    private static ButtonCaptionList? ReadList(FwxDocument doc, TableItem item, long body, string section, ButtonMode mode)
    {
        if (mode is not (ButtonMode.TextList or ButtonMode.GraphicList))
        {
            return null;
        }
        try
        {
            return ButtonListReader.Read(doc, item, body, section, mode);
        }
        catch (FwxFormatException error)
        {
            doc.Warnings.Add(error.Message);
            return new ButtonCaptionList(null, null);
        }
    }
}
