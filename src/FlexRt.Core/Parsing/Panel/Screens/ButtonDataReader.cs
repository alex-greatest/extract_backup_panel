using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Проверка границ и ссылок данных General кнопки.</summary>
internal static class ButtonDataReader
{
    /// <summary>Проверить диапазон внутри элемента, не затрагивая соседние записи.</summary>
    /// <exception cref="FwxFormatException">Данные выходят за границы элемента.</exception>
    public static void Check(TableItem item, long offset, long length, string section)
    {
        if (offset < 0 || length < 0 || offset > item.Length || length > item.Length - offset)
        {
            throw new FwxFormatException(section, item.Offset + offset, $"запись {item.Index + 1}: свойства кнопки выходят за границы элемента");
        }
    }

    /// <summary>Проверить номер строки; ноль означает отсутствие текста.</summary>
    /// <returns>Номер строки или null.</returns>
    /// <exception cref="FwxFormatException">Номер отсутствует в STRINGSTORE.</exception>
    public static int? TextIndex(FwxDocument doc, long value, string section, long offset)
    {
        if (value == 0)
        {
            return null;
        }
        if (value > int.MaxValue || !doc.Strings.Any(s => s.Idx == value))
        {
            throw new FwxFormatException(section, offset, $"строка подписи кнопки {value} отсутствует в STRINGSTORE");
        }
        return (int)value;
    }

    /// <summary>Прочитать пару номер элемента / Id таблицы картинки и проверить её цель.</summary>
    /// <returns>Ссылка на картинку.</returns>
    /// <exception cref="FwxFormatException">Цели нет, таблица не PICTURE или элемент вне таблицы.</exception>
    public static VarReference Picture(FwxDocument doc, long offset, string section)
    {
        var b = doc.Binary;
        var link = new VarReference(b.D2(offset + 2), b.D2(offset));
        var table = doc.FindTable(link.Table);
        if (table is null || table.Name != "PICTURE")
        {
            throw new FwxFormatException(section, offset, $"ссылка картинки кнопки на таблицу Id 0x{link.Table:X} не разобрана");
        }
        ElementLink.Item(doc, table, link.Index, "кнопка");
        return link;
    }
}
