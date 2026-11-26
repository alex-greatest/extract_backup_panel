using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens.Buttons;

/// <summary>Общие проверки границ, чтение ссылок и обработка ошибок компонентов кнопки.</summary>
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
        if (value > int.MaxValue || doc.Strings.All(s => s.Idx != value))
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

    /// <summary>Прочитать ссылку оформления; ClassId 0 и InstanceId 0xffff означают отсутствие компонента.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица владельца ссылки.</param>
    /// <param name="item">Запись владельца ссылки.</param>
    /// <param name="offset">Смещение ссылки от начала записи.</param>
    /// <param name="name">Ожидаемая таблица цели.</param>
    /// <param name="instanceFirst">Порядок InstanceId, ClassId у ссылки FONT после метки подписей.</param>
    /// <returns>Ссылка или null.</returns>
    /// <exception cref="FwxFormatException">Ссылка выходит за запись, таблица цели неверна или номер элемента не существует.</exception>
    public static VarReference? StyleReference(FwxDocument doc, TocEntry table, TableItem item, long offset, string name,
        bool instanceFirst = false)
    {
        Check(item, offset, 4, table.Name);
        var p = item.Offset + offset;
        var link = new VarReference(doc.Binary.D2(p + (instanceFirst ? 2 : 0)), doc.Binary.D2(p + (instanceFirst ? 0 : 2)));
        if (link is { Table: 0, Index: 0xffff })
        {
            return null;
        }
        var target = doc.FindTable(link.Table);
        if (target is null || target.Name != name)
        {
            throw new FwxFormatException(table.Name, p, $"ссылка оформления кнопки на {name} не разобрана (Id 0x{link.Table:X})");
        }
        ElementLink.Item(doc, target, link.Index, "оформление кнопки");
        return link;
    }

    /// <summary>Сохранить ошибку компонента в предупреждениях и вернуть значение отсутствующего компонента.</summary>
    /// <returns>Прочитанное значение или <paramref name="missing"/> при ошибке формата.</returns>
    public static T ReadValue<T>(FwxDocument doc, Func<T> read, T missing)
    {
        try
        {
            return read();
        }
        catch (FwxFormatException error)
        {
            if (!doc.Warnings.Contains(error.Message))
            {
                doc.Warnings.Add(error.Message);
            }
            return missing;
        }
    }
}
