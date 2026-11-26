using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens.Buttons.Captions;

/// <summary>Подписи кнопки: записи по 8 байт до метки, опыты steps-buttons V21.</summary>
internal static class ButtonCaptionReader
{
    /// <summary>Текст ненажатого состояния.</summary>
    private const long OffText = 4;

    /// <summary>Текст нажатого состояния.</summary>
    private const long OnText = 3;

    /// <summary>Ссылка картинки ненажатого состояния.</summary>
    private const long OffPicture = 0x20000006;

    /// <summary>Ссылка картинки нажатого состояния.</summary>
    private const long OnPicture = 0x20000005;

    /// <summary>Метка конца подписей и начала ссылки на FONT.</summary>
    private const long EndMarker = 0x9FFFFE00;

    /// <summary>Прочитать текст и картинки обоих состояний после блока стилей.</summary>
    /// <returns>Подписи ненажатого и нажатого состояний.</returns>
    /// <exception cref="FwxFormatException">Блок, дескриптор или ссылка подписи не разобраны.</exception>
    public static (ButtonCaption Off, ButtonCaption On) Read(FwxDocument doc, TableItem item, long cursor, string section)
    {
        var b = doc.Binary;
        var off = new ButtonCaption(null, null);
        var on = new ButtonCaption(null, null);
        var seen = new HashSet<long>();
        while (true)
        {
            ButtonDataReader.Check(item, cursor, 4, section);
            var descriptor = b.D4(item.Offset + cursor);
            if (descriptor == EndMarker)
            {
                return (off, on);
            }
            ButtonDataReader.Check(item, cursor, 8, section);
            var value = item.Offset + cursor + 4;
            if (!seen.Add(descriptor))
            {
                throw new FwxFormatException(section, item.Offset + cursor, "повторный дескриптор подписи кнопки");
            }
            switch (descriptor)
            {
                case OffText:
                    off = off with { TextIndex = ButtonDataReader.TextIndex(doc, b.D4(value), section, value) };
                    break;
                case OnText:
                    on = on with { TextIndex = ButtonDataReader.TextIndex(doc, b.D4(value), section, value) };
                    break;
                case OffPicture:
                    off = off with { Picture = ButtonDataReader.Picture(doc, value, section) };
                    break;
                case OnPicture:
                    on = on with { Picture = ButtonDataReader.Picture(doc, value, section) };
                    break;
                default:
                    throw new FwxFormatException(section, item.Offset + cursor, $"дескриптор подписи кнопки 0x{descriptor:X} не разобран");
            }
            cursor += 8;
        }
    }
}
