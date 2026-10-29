using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel.Alarms;
using FlexRt.Parsing.Panel.Pictures;
using FlexRt.Parsing.Panel.Screens;

namespace FlexRt.Parsing.Panel;

/// <summary>Каркас FWX: заголовок файла, TOC и обход элементов любой таблицы.</summary>
public static class FwxReader
{
    /// <summary>Размер заголовка таблицы; сразу за ним — каталог смещений элементов.</summary>
    public const int TableHeaderSize = 0x34;

    /// <summary>
    /// Прочитать файл целиком. Ошибка в заголовке или TOC прерывает чтение;
    /// ошибка в отдельной таблице попадает в <see cref="FwxDocument.Warnings"/>,
    /// чтобы остальные таблицы разбирались и панель показывалась.
    /// После TOC разбираются таблицы STRINGSTORE, VAR, DATALINK_READWR, DATALINK, CONNECTION_OMSP, DEVICE_OMSP,
    /// SYSMSGHANDLER, MSG_NUMHANDLER, MSG_NUMEVENT, AREAPTR, MSG_ANALOGHANDL, MSG_ANALOGCREAT,
    /// MSG_ANALOG_OBSE, RECMSG, ARCHIVES, MSG_LOGGER, FUNC_SEND_EMAIL, SCREEN, LIMIT, ADAPTVALUECHANG, GUI,
    /// INFO_HANDLER, PICTURE и TILED_PICTURE; последней — раскладка всех таблиц TOC (<see cref="TableLayoutCheck"/>).
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
        TryParse(doc, SysMsgHandlerParser.Parse);
        TryParse(doc, MsgNumHandlerParser.Parse);
        TryParse(doc, MsgNumEventParser.Parse);
        TryParse(doc, AreaPtrParser.Parse);
        TryParse(doc, MsgAnalogHandlParser.Parse);
        TryParse(doc, MsgAnalogCreatParser.Parse);
        TryParse(doc, MsgAnalogObseParser.Parse);
        TryParse(doc, RecMsgParser.Parse);
        TryParse(doc, ArchivesParser.Parse);
        TryParse(doc, MsgLoggerParser.Parse);
        TryParse(doc, FuncSendEmailParser.Parse);
        TryParse(doc, ScreenParser.Parse);
        // только после ScreenParser: читаются элементы LIMIT, на которые ссылаются записи из doc.Screens
        TryParse(doc, LimitParser.Parse);
        TryParse(doc, AdaptValueChangParser.Parse);
        TryParse(doc, GuiParser.Parse);
        // только после ScreenParser: элемент VIEW_DYN выбирается по ссылке записи Overview из doc.Screens
        TryParse(doc, ViewDynParser.Parse);
        TryParse(doc, InfoHandlerParser.Parse);
        TryParse(doc, PictureParser.Parse);
        TryParse(doc, TiledPictureParser.Parse);
        TableLayoutCheck.Run(doc);
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
        for (var i = 0; i < table.Entries; i++)
        {
            yield return Item(b, table, i);
        }
    }

    /// <summary>
    /// Элемент таблицы по номеру, как в <see cref="Items"/>, но без обхода остальных: смещения читаются
    /// только у него и у следующего, поэтому испорченная запись каталога у другого элемента не мешает.
    /// Номер должен быть меньше <see cref="TocEntry.Entries"/>. Выставляет <see cref="FwxBinary.Section"/>
    /// в имя таблицы.
    /// </summary>
    /// <returns>Номер, начало и длина элемента.</returns>
    /// <exception cref="FwxFormatException">Смещение вне файла или отрицательная длина элемента.</exception>
    public static TableItem Item(FwxBinary b, TocEntry table, int index)
    {
        var offsets = table.Offset + TableHeaderSize;
        var dataBlock = offsets + table.Entries * 4L;
        b.Section = table.Name;
        var start = dataBlock + b.D4(offsets + index * 4L);
        var end = index < table.Entries - 1 ? dataBlock + b.D4(offsets + (index + 1) * 4L) : table.Offset + table.Size;
        return end >= start
            ? new TableItem(index, start, end - start)
            : throw new FwxFormatException(table.Name, start, $"отрицательная длина элемента {index + 1}");
    }

    /// <summary>
    /// Как <see cref="Items"/>, но каждый элемент перед выдачей проверяется
    /// <see cref="CheckInsideTable"/>: для таблиц, где один плохой элемент обрывает разбор.
    /// </summary>
    /// <exception cref="FwxFormatException">Смещение вне файла, отрицательная длина или элемент за пределами таблицы.</exception>
    public static IEnumerable<TableItem> CheckedItems(FwxBinary b, TocEntry table)
    {
        for (var i = 0; i < table.Entries; i++)
        {
            yield return CheckedItem(b, table, i);
        }
    }

    /// <summary>
    /// Как <see cref="Item"/>, но элемент перед выдачей проверяется <see cref="CheckInsideTable"/>.
    /// Номер должен быть меньше <see cref="TocEntry.Entries"/>.
    /// </summary>
    /// <returns>Номер, начало и длина элемента.</returns>
    /// <exception cref="FwxFormatException">Смещение вне файла, отрицательная длина или элемент за пределами таблицы.</exception>
    public static TableItem CheckedItem(FwxBinary b, TocEntry table, int index)
    {
        var item = Item(b, table, index);
        CheckInsideTable(table, item);
        return item;
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
