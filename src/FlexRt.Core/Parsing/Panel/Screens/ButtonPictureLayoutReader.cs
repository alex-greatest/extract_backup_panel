using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Поля и выравнивание графической подписи кнопки из PS_Position.</summary>
internal static class ButtonPictureLayoutReader
{
    /// <summary>Имя таблицы.</summary>
    public const string Table = "PS_Position";

    /// <summary>Четыре байта полей: Top, Bottom, Left, Right.</summary>
    private const int MarginsOffset = 0x14;

    /// <summary>Байт горизонтального выравнивания.</summary>
    private const int HorizontalOffset = 0x1c;

    /// <summary>Байт вертикального выравнивания.</summary>
    private const int VerticalOffset = 0x1d;

    /// <summary>Наименьшая длина записи до конца выравнивания.</summary>
    private const int MinLength = VerticalOffset + 1;

    /// <summary>Прочитать поля и выравнивание картинки по ссылке состояния кнопки.</summary>
    /// <returns>Положение графической подписи.</returns>
    /// <exception cref="FwxFormatException">Запись короткая или код выравнивания неизвестен.</exception>
    public static ButtonPictureLayout Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, MinLength, owner);
        var b = doc.Binary;
        var p = item.Offset;
        var horizontal = b.D1(p + HorizontalOffset);
        var vertical = b.D1(p + VerticalOffset);
        if (horizontal > 2)
        {
            throw new FwxFormatException(table.Name, p + HorizontalOffset,
                $"горизонтальное выравнивание картинки кнопки 0x{horizontal:X} не разобрано");
        }
        if (vertical > 2)
        {
            throw new FwxFormatException(table.Name, p + VerticalOffset,
                $"вертикальное выравнивание картинки кнопки 0x{vertical:X} не разобрано");
        }
        var margins = new ButtonPictureMargins(
            b.D1(p + MarginsOffset),
            b.D1(p + MarginsOffset + 1),
            b.D1(p + MarginsOffset + 2),
            b.D1(p + MarginsOffset + 3));
        return new ButtonPictureLayout(margins, horizontal, vertical);
    }
}
