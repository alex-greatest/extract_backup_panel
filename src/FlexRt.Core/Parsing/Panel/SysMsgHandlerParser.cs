using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор SYSMSGHANDLER: системные события панели — номер события и номер строки его текста.</summary>
public static class SysMsgHandlerParser
{
    /// <summary>
    /// Смещение числа событий (u16) в элементе. Поля до него не разобраны; наблюдение: 585 в
    /// Project1 и A603A0097 (TIA V21 / TP700), 580 в копиях A603A0097, скомпилированных раньше.
    /// </summary>
    private const int CountOffset = 0x2a;

    /// <summary>Начало списка событий: пары u32 (номер события, номер строки текста).</summary>
    private const int PairsOffset = 0x2c;

    /// <summary>Размер одной пары.</summary>
    private const int PairSize = 8;

    /// <summary>
    /// Прочитать таблицу SYSMSGHANDLER и добавить события в <see cref="FwxDocument.SystemEvents"/>
    /// в порядке файла. Элементов в файлах один; если их несколько, события всех элементов идут
    /// подряд в один список. Нет таблицы в TOC — ничего не делает. Байты после списка не разбираются:
    /// в Project1 их нет, в A603A0097 — 40 байт неизвестной структуры.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или список событий — за пределы элемента.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("SYSMSGHANDLER");
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
    /// Один элемент таблицы: число событий на <see cref="CountOffset"/>, с <see cref="PairsOffset"/> —
    /// пары (номер события, номер строки текста). Каждое событие сразу добавляется в документ.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент короче заголовка или список событий не помещается в элемент.</exception>
    private static void ReadRecord(FwxDocument doc, TableItem item)
    {
        var b = doc.Binary;
        if (item.Length < PairsOffset)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {item.Index + 1}: длина {item.Length} меньше заголовка 0x{PairsOffset:x}");
        }
        var count = b.D2(item.Offset + CountOffset);
        if (PairsOffset + (long)count * PairSize > item.Length)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {item.Index + 1}: {count} событий не помещаются в {item.Length} байт");
        }
        for (var i = 0; i < count; i++)
        {
            var pair = item.Offset + PairsOffset + i * PairSize;
            doc.SystemEvents.Add(new SystemEvent(b.D4(pair), b.D4(pair + 4)));
        }
    }
}
