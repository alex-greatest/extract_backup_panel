using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор INFO_HANDLER: тексты-подсказки элементов других таблиц — пары (чей элемент, номер строки языков).</summary>
public static class InfoHandlerParser
{
    /// <summary>Первое слово элемента (как у записи VAR).</summary>
    private const int Marker = 3;

    /// <summary>
    /// Смещение u32 числа текстов от конца имени; перед ним u32 (наблюдение: во всех файлах 0, назначение неизвестно).
    /// </summary>
    private const int CountOffset = 4;

    /// <summary>Смещение ключей от конца имени: за u32 (0) и u32 числом текстов.</summary>
    private const int KeysOffset = 8;

    /// <summary>Размер ключа: u16 номер элемента, u16 Id таблицы.</summary>
    private const int KeySize = 4;

    /// <summary>Размер номера строки языков: u32.</summary>
    private const int TextIndexSize = 4;

    /// <summary>
    /// Прочитать таблицу INFO_HANDLER и добавить тексты в <see cref="FwxDocument.InfoTexts"/> в порядке файла.
    /// Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или не сходится с раскладкой.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("INFO_HANDLER");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            ReadRecord(doc, item);
        }
    }

    /// <summary>
    /// Элемент (сверено с подсказками в TIA на шаговых копиях Project1 и A603A0097, V21; в файлах элемент один,
    /// имя <c>InfoTextHandler</c>): u16 3, u16 длина имени в символах, имя UTF-16LE, нули до кратности 4, u32 (0),
    /// u32 N, N ключей подряд (u16 номер элемента, u16 Id таблицы), затем N номеров строк языков (u32) в том же
    /// порядке. Имя и u32 (0) не читаются. Каждый текст сразу добавляется в документ.
    /// </summary>
    /// <exception cref="FwxFormatException">
    /// Элемент короче 4 байт, первое слово не 3 или имя, число текстов, ключи и номера строк не помещаются в элемент.
    /// </exception>
    private static void ReadRecord(FwxDocument doc, TableItem item)
    {
        var b = doc.Binary;
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < 4)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 4");
        }
        var marker = b.D2(p);
        if (marker != Marker)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: первое слово {marker}, ожидалось 3");
        }
        var nameEnd = (4 + b.D2(p + 2) * 2L + 3) / 4 * 4;
        var keys = nameEnd + KeysOffset;
        if (keys > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: число текстов не помещается в {item.Length} байт");
        }
        var count = b.D4(p + nameEnd + CountOffset);
        var texts = keys + count * KeySize;
        if (texts + count * TextIndexSize > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: {count} текстов не помещаются в {item.Length} байт");
        }
        for (var i = 0L; i < count; i++)
        {
            var key = p + keys + i * KeySize;
            var owner = new VarReference(b.D2(key + 2), b.D2(key));
            doc.InfoTexts.Add(new InfoText(owner, (int)b.D4(p + texts + i * TextIndexSize)));
        }
    }
}
