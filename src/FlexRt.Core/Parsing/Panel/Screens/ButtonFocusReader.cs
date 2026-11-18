using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Стиль PS_Focus кнопки, подтверждённый шаговыми компиляциями TP700 Comfort в TIA V21.</summary>
internal static class ButtonFocusReader
{
    /// <summary>Имя таблицы стиля.</summary>
    public const string Table = "PS_Focus";

    /// <summary>R, G, B рамки фокуса.</summary>
    private const int ColorOffset = 0x14;

    /// <summary>Ширина рамки фокуса, u16.</summary>
    private const int WidthOffset = 0x18;

    /// <summary>Наименьшая длина элемента: до конца поля ширины.</summary>
    private const int MinLength = WidthOffset + 2;

    /// <summary>Прочитать ширину и цвет фокуса по ссылке кнопки.</summary>
    /// <returns>Оформление фокуса.</returns>
    /// <exception cref="FwxFormatException">Запись стиля отсутствует или выходит за границы таблицы, либо короче необходимого.</exception>
    public static ButtonFocus Read(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = StyleElement.Item(doc, table, index, MinLength, owner);
        var binary = doc.Binary;
        return new ButtonFocus(binary.D2(item.Offset + WidthOffset), StyleElement.Rgb(binary, item.Offset + ColorOffset));
    }
}
