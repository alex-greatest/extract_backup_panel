using FlexRt.Binary;
using FlexRt.Model;

namespace FlexRt.Parsing;

/// <summary>Разбор VAR: теги HMI — имя, код типа, число элементов.</summary>
public static class VarParser
{
    /// <summary>Первое слово записи; на всех внутренних тегах TIA V17 Comfort — 3, PLC-теги не исследованы.</summary>
    private const int RecordKind = 3;

    /// <summary>Размер хвоста записи: код типа, число элементов, <c>Data4</c>, нулевое слово.</summary>
    private const int TailSize = 0xc;

    /// <summary>
    /// Прочитать таблицу VAR и добавить теги в <see cref="FwxDocument.Tags"/> в порядке
    /// каталога. Нет таблицы в TOC — ничего не делает. Каждый тег добавляется сразу после
    /// чтения: если разбор оборвётся, уже прочитанные теги остаются в документе.
    /// </summary>
    /// <exception cref="FwxFormatException">Запись выходит за пределы таблицы VAR или не сходится с раскладкой.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("VAR");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.Items(doc.Binary, table))
        {
            CheckInsideTable(table, item);
            doc.Tags.Add(ReadRecord(doc.Binary, item));
        }
    }

    /// <summary>
    /// Проверить, что запись целиком лежит в блоке данных таблицы VAR: от конца каталога
    /// смещений до конца таблицы. Байты соседних таблиц в тег не попадают.
    /// </summary>
    /// <exception cref="FwxFormatException">Запись начинается или кончается за пределами таблицы.</exception>
    private static void CheckInsideTable(TocEntry table, TableItem item)
    {
        var dataBlock = table.Offset + FwxReader.TableHeaderSize + table.Entries * 4L;
        var tableEnd = table.Offset + table.Size;
        if (item.Offset < dataBlock || item.Offset + item.Length > tableEnd)
        {
            throw new FwxFormatException("VAR", item.Offset, $"запись {item.Index + 1} выходит за пределы таблицы");
        }
    }

    /// <summary>
    /// Одна запись: <c>+0x00</c> слово 3, <c>+0x02</c> длина имени в символах, <c>+0x04</c>
    /// имя UTF-16LE. Имя дополнено нулями до кратности 4 и ещё 4 нулевыми байтами, за ними
    /// хвост из 12 байт: код типа (2), число элементов (2), <c>Data4</c> (4), нулевое слово (4).
    /// Хвост кончается ровно на конце элемента.
    /// </summary>
    /// <returns>Тег.</returns>
    /// <exception cref="FwxFormatException">
    /// Первое слово не 3, длина записи не сходится с длиной имени, между именем и хвостом
    /// не нули, последнее слово не 0 или у массива 0 элементов.
    /// </exception>
    private static HmiTag ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var number = item.Index + 1;

        var kind = b.D2(p);
        if (kind != RecordKind)
        {
            throw new FwxFormatException("VAR", p, $"запись {number}: первое слово 0x{kind:x}, ожидалось 0x{RecordKind:x}");
        }

        var chars = b.D2(p + 2);
        var nameEnd = 4L + chars * 2;
        var tail = (nameEnd + 3) / 4 * 4 + 4;
        if (tail + TailSize != item.Length)
        {
            throw new FwxFormatException("VAR", p, $"запись {number}: длина 0x{item.Length:x}, по имени из {chars} символов ожидалось 0x{tail + TailSize:x}");
        }

        CheckZeros(b, p + nameEnd, tail - nameEnd, number);
        var name = b.GetNameLen(p + 4, chars);
        var typeCode = b.D2(p + tail);
        var elements = b.D2(p + tail + 2);
        var data4 = b.D4(p + tail + 4);
        // всегда 0
        var last = b.D4(p + tail + 8);
        if (last != 0)
        {
            throw new FwxFormatException("VAR", p + tail + 8, $"запись {number}: последнее слово 0x{last:x}, ожидалось 0");
        }
        if ((typeCode & HmiTag.ArrayFlag) != 0 && elements == 0)
        {
            throw new FwxFormatException("VAR", p + tail + 2, $"запись {number}: массив без элементов");
        }

        return new HmiTag(number, name, typeCode, elements, data4, p);
    }

    /// <summary>Проверить, что между концом имени и хвостом записи только нулевые байты.</summary>
    /// <exception cref="FwxFormatException">Найден ненулевой байт.</exception>
    private static void CheckZeros(FwxBinary b, long pos, long length, int number)
    {
        for (var i = 0L; i < length; i++)
        {
            if (b.D1(pos + i) != 0)
            {
                throw new FwxFormatException("VAR", pos + i, $"запись {number}: ненулевой байт между именем и хвостом");
            }
        }
    }
}
