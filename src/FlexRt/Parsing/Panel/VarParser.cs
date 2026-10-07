using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор VAR: теги HMI — имя, код типа, число элементов, у PLC-тега — ссылка на связь с ПЛК.</summary>
public static class VarParser
{
    /// <summary>Первое слово записи; на всех тегах TIA V17 и V21 Comfort — 3.</summary>
    private const int RecordKind = 3;

    /// <summary>Размер хвоста записи: код типа, число элементов, <c>Data4</c>, нулевое слово.</summary>
    private const int TailSize = 0xc;

    /// <summary>Размер одной ссылки в блоке после имени: Id таблицы, номер элемента и два слова.</summary>
    private const int LinkSize = 8;

    /// <summary>Наибольшее число ссылок в записи, которое принимается без ошибки (наблюдение: 0..3).</summary>
    private const long MaxLinks = 0x10;

    /// <summary>
    /// Прочитать таблицу VAR и добавить теги в <see cref="FwxDocument.Tags"/> в порядке
    /// каталога. Нет таблицы в TOC — ничего не делает. Запись, которую не удалось прочитать,
    /// пропускается, разбор продолжается; после таблицы в <see cref="FwxDocument.Warnings"/>
    /// добавляется одно сообщение с числом пропущенных записей и одно — с числом тегов,
    /// связанных не через DATALINK_READWR и не через DATALINK (у них данные ПЛК неизвестны).
    /// </summary>
    /// <exception cref="FwxFormatException">Каталог смещений таблицы VAR испорчен.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("VAR");
        if (table is null)
        {
            return;
        }

        var skipped = new List<string>();
        var datalinkId = doc.FindTable("DATALINK_READWR")?.Id;
        foreach (var item in FwxReader.Items(doc.Binary, table))
        {
            try
            {
                FwxReader.CheckInsideTable(table, item);
                doc.Tags.Add(ReadRecord(doc.Binary, item, datalinkId));
            }
            catch (FwxFormatException e)
            {
                skipped.Add(e.Message);
            }
        }
        AddWarnings(doc, skipped);
    }

    /// <summary>
    /// Одно сводное предупреждение по таблице, если есть что сообщить: пропущенные записи
    /// (число и первая причина) и теги со ссылкой не на DATALINK_READWR и не на DATALINK (число
    /// и Id таблиц).
    /// </summary>
    private static void AddWarnings(FwxDocument doc, List<string> skipped)
    {
        var parts = new List<string>();
        if (skipped.Count > 0)
        {
            parts.Add($"пропущено записей с незнакомой раскладкой: {skipped.Count}, первая — {skipped[0]}");
        }
        int?[] known = [doc.FindTable("DATALINK_READWR")?.Id, doc.FindTable("DATALINK")?.Id];
        var foreign = doc.Tags.Where(t => t.LinkTable is not null && !known.Contains(t.LinkTable)).ToList();
        if (foreign.Count > 0)
        {
            var tables = string.Join(", ", foreign.Select(t => $"0x{t.LinkTable:x}").Distinct());
            parts.Add($"тегов со связью не через DATALINK_READWR и DATALINK (таблицы {tables}): {foreign.Count}, данные ПЛК для них неизвестны");
        }
        if (parts.Count > 0)
        {
            doc.Warnings.Add("VAR: " + string.Join("; ", parts));
        }
    }

    /// <summary>
    /// Одна запись: <c>+0x00</c> слово 3, <c>+0x02</c> длина имени в символах, <c>+0x04</c>
    /// имя UTF-16LE, нули до кратности 4, u32 N — число ссылок, N ссылок по 8 байт
    /// <c>[u16 Id таблицы][u16 номер элемента][u16][u16]</c>, хвост из 12 байт: код типа (2),
    /// число элементов (2), <c>Data4</c> (4), нулевое слово (4). Хвост кончается ровно на
    /// конце элемента. Внутренний тег: N = 0. PLC-тег: среди ссылок есть ссылка на
    /// DATALINK_READWR; другие ссылки (указатели аварий, обратные ссылки) не используются.
    /// Ссылка с Id таблицы 0 — завершающая.
    /// </summary>
    /// <returns>Тег.</returns>
    /// <exception cref="FwxFormatException">
    /// Первое слово не 3, длина записи не сходится с N, после имени не нули,
    /// последнее слово не 0 или у массива 0 элементов.
    /// </exception>
    private static HmiTag ReadRecord(FwxBinary b, TableItem item, int? datalinkId)
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
        var aligned = (nameEnd + 3) / 4 * 4;
        CheckZeros(b, p + nameEnd, aligned - nameEnd, number);
        var links = b.D4(p + aligned);
        var tail = aligned + 4 + links * LinkSize;
        if (links > MaxLinks || tail + TailSize != item.Length)
        {
            throw new FwxFormatException("VAR", p, $"запись {number}: длина 0x{item.Length:x} не сходится с именем из {chars} символов и {links} ссылками");
        }

        var (linkTable, linkIndex) = PickLink(b, p + aligned + 4, (int)links, datalinkId);
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
        return new HmiTag(number, name, typeCode, elements, data4, p, linkTable, linkIndex);
    }

    /// <summary>
    /// Выбрать ссылку тега: ссылку на DATALINK_READWR, если она есть; иначе первую
    /// незавершающую (Id таблицы не 0); нет ссылок — внутренний тег.
    /// </summary>
    /// <returns>Id таблицы и номер элемента или (<c>null</c>, <c>null</c>) у внутреннего тега.</returns>
    private static (int? Table, int? Index) PickLink(FwxBinary b, long pos, int count, int? datalinkId)
    {
        (int? Table, int? Index) first = (null, null);
        for (var i = 0; i < count; i++)
        {
            var table = b.D2(pos + i * LinkSize);
            var index = b.D2(pos + i * LinkSize + 2);
            if (table == datalinkId)
            {
                return (table, index);
            }
            if (table != 0 && first.Table is null)
            {
                first = (table, index);
            }
        }
        return first;
    }

    /// <summary>Проверить, что в диапазоне после имени только нулевые байты.</summary>
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
