using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Каркас FWX: заголовок файла, TOC и обход элементов любой таблицы.</summary>
public static class FwxReader
{
    /// <summary>Размер заголовка таблицы; сразу за ним — каталог смещений элементов.</summary>
    public const int TableHeaderSize = 0x34;

    /// <summary>
    /// Прочитать файл целиком. Ошибка в заголовке или TOC прерывает чтение;
    /// ошибка в отдельной таблице попадает в <see cref="FwxDocument.Warnings"/>,
    /// чтобы остальные экспорты (xlsx, explode) могли отработать.
    /// После TOC разбираются таблицы STRINGSTORE, VAR, DATALINK_READWR, DATALINK, CONNECTION_OMSP и DEVICE_OMSP.
    /// </summary>
    /// <exception cref="FwxFormatException">Повреждён заголовок или TOC.</exception>
    /// <exception cref="IOException">Файл не удалось прочитать.</exception>
    public static FwxDocument Read(string path)
    {
        var b = new FwxBinary(File.ReadAllBytes(path));
        var doc = new FwxDocument { Binary = b, Header = ReadHeader(b) };

        ReadToc(doc);
        TryParse(doc, StringStoreParser.Parse);
        TryParse(doc, VarParser.Parse);
        TryParse(doc, DatalinkReadWrParser.Parse);
        TryParse(doc, DatalinkReadWrParser.ParseAreaPointers);
        TryParse(doc, ConnectionParser.Parse);
        TryParse(doc, DeviceParser.Parse);
        return doc;
    }

    /// <summary>
    /// Вызвать парсер таблицы; ошибку формата превратить в предупреждение
    /// <see cref="FwxDocument.Warnings"/>, чтобы сбой одной таблицы не отменял остальные.
    /// Что успел прочитать парсер до ошибки, остаётся в документе.
    /// </summary>
    private static void TryParse(FwxDocument doc, Action<FwxDocument> parse)
    {
        try
        {
            parse(doc);
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add(e.Message);
        }
    }

    /// <summary>
    /// Элементы таблицы: за заголовком идёт каталог смещений (4 байта на элемент),
    /// за ним — блок данных, смещения считаются от его начала.
    /// Длина элемента — до начала следующего, у последнего — до конца таблицы.
    /// Выставляет <see cref="FwxBinary.Section"/> в имя таблицы.
    /// </summary>
    /// <exception cref="FwxFormatException">Смещение вне файла или отрицательная длина элемента.</exception>
    public static IEnumerable<TableItem> Items(FwxBinary b, TocEntry table)
    {
        var offsets = table.Offset + TableHeaderSize;
        var dataBlock = offsets + table.Entries * 4L;
        var tableEnd = table.Offset + table.Size;
        b.Section = table.Name;

        for (var i = 0; i < table.Entries; i++)
        {
            var start = dataBlock + b.D4(offsets + i * 4L);
            var end = i < table.Entries - 1 ? dataBlock + b.D4(offsets + (i + 1) * 4L) : tableEnd;
            if (end < start)
            {
                throw new FwxFormatException(table.Name, start, $"отрицательная длина элемента {i + 1}");
            }
            yield return new TableItem(i, start, end - start);
        }
    }

    /// <summary>
    /// Как <see cref="Items"/>, но каждый элемент перед выдачей проверяется
    /// <see cref="CheckInsideTable"/>: для таблиц, где один плохой элемент обрывает разбор.
    /// </summary>
    /// <exception cref="FwxFormatException">Смещение вне файла, отрицательная длина или элемент за пределами таблицы.</exception>
    public static IEnumerable<TableItem> CheckedItems(FwxBinary b, TocEntry table)
    {
        foreach (var item in Items(b, table))
        {
            CheckInsideTable(table, item);
            yield return item;
        }
    }

    /// <summary>
    /// Проверить, что элемент целиком лежит в блоке данных своей таблицы: от конца каталога
    /// смещений до конца таблицы. Байты соседних таблиц в запись не попадают.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент начинается или кончается за пределами таблицы.</exception>
    public static void CheckInsideTable(TocEntry table, TableItem item)
    {
        var dataBlock = table.Offset + TableHeaderSize + table.Entries * 4L;
        var tableEnd = table.Offset + table.Size;
        if (item.Offset < dataBlock || item.Offset + item.Length > tableEnd)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1} выходит за пределы таблицы");
        }
    }

    /// <summary>Заголовок файла: сигнатура 0xbeef и смещения служебных массивов.</summary>
    /// <exception cref="FwxFormatException">Неверная сигнатура или файл короче заголовка.</exception>
    private static FwxHeader ReadHeader(FwxBinary b)
    {
        b.Section = "HEADER";
        var header = new FwxHeader(
            // первые 6 слов всегда одинаковые (TIA V17): 0xbeef, 0xc, 0x0, 0x1100, 0x1, 0x1
            b.D2(0), b.D2(2), b.D2(4), b.D2(6), b.D2(8), b.D2(0xa),
            // гипотеза: размер буфера значений тегов сразу за таблицами; оба поля всегда равны
            b.D4(0xc), b.D4(0x10),
            // конец таблиц — по нему считается длина последней таблицы
            b.D4(0x14),
            // Table Of Contents
            b.D4(0x18), b.D4(0x1c),
            // какие элементы инициализировать при старте runtime (предположительно)
            b.D4(0x20), b.D4(0x24),
            b.D4(0x28), b.D4(0x2c),
            b.D4(0x30), b.D4(0x34),
            // языки (0x409 - English, 0x407 - German, ...)
            b.D4(0x38), b.D4(0x3c)
        );

        return header.Start1 == 0xbeef
            ? header
            : throw new FwxFormatException("HEADER", 0, $"неверная сигнатура 0x{header.Start1:x}, ожидалось 0xbeef");
    }

    /// <summary>
    /// Table Of Contents: массив смещений таблиц, 0 — пустая запись.
    /// Размеры таблиц проставляет <see cref="SetTableSizes"/>.
    /// </summary>
    private static void ReadToc(FwxDocument doc)
    {
        var b = doc.Binary;
        var header = doc.Header;
        b.Section = "TOC";
        b.Check(header.TocOffset, header.TocEntries * 4);

        for (var i = 0L; i < header.TocEntries; i++)
        {
            var offset = b.D4(header.TocOffset + i * 4);
            if (offset == 0)
            {
                continue;
            }

            var entry = new TocEntry
            {
                Entries = b.D2(offset),      // сколько элементов в таблице
                Version = b.D2(offset + 2),  // у всех таблиц 0x64 (версия 1.00?)
                Parent = b.D2(offset + 4),   // у всех таблиц 0x01
                Id = b.D2(offset + 6),       // совпадает с индексом в TOC
                Name = b.GetName(offset + 8),
                Offset = offset
            };
            doc.Toc.Add(entry);
        }

        SetTableSizes(doc.Toc, header.TablesEnd);
    }

    /// <summary>
    /// Проставить размер каждой таблицы: до следующей по физическому порядку смещений
    /// (порядок в TOC может с ним не совпадать), у последней — до
    /// <paramref name="tablesEnd"/> (<see cref="FwxHeader.TablesEnd"/>).
    /// </summary>
    private static void SetTableSizes(List<TocEntry> toc, long tablesEnd)
    {
        var physical = toc.OrderBy(t => t.Offset).ToList();
        for (var i = 0; i < physical.Count; i++)
        {
            var end = i + 1 < physical.Count ? physical[i + 1].Offset : tablesEnd;
            physical[i].Size = end - physical[i].Offset;
        }
    }
}
