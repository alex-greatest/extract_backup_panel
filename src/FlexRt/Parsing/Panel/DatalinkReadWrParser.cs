using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор DATALINK_READWR: связь каждого PLC-тега с ПЛК.</summary>
public static class DatalinkReadWrParser
{
    /// <summary>Длина элемента при абсолютном доступе.</summary>
    private const int AbsoluteSize = 0x30;

    /// <summary>Длина элемента при символьном доступе без пути: 0x34 + 4 байта на уровень пути.</summary>
    private const int SymbolicBaseSize = 0x34;

    /// <summary>Длина общей части элемента, которая читается у любого элемента.</summary>
    private const int CommonSize = 0x20;

    /// <summary>Флаг ID символа I/Q/M/C/T (наблюдение: всегда 0x40000000); в проекте ПЛК его нет.</summary>
    private const long SymbolIdFlag = 0x40000000;

    /// <summary>
    /// Прочитать таблицу DATALINK_READWR и добавить связи в <see cref="FwxDocument.Links"/>
    /// в порядке каталога. Нет таблицы в TOC — ничего не делает. Элемент незнакомой длины или
    /// области добавляется с <see cref="PlcLink.Decoded"/> = <c>false</c>; если такие есть,
    /// после таблицы в <see cref="FwxDocument.Warnings"/> добавляется одно сообщение с их числом.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или короче общей части.</exception>
    public static void Parse(FwxDocument doc)
    {
        ReadTable(doc, "DATALINK_READWR", doc.Links);
        var undecoded = doc.Links.Count(l => !l.Decoded);
        if (undecoded > 0)
        {
            doc.Warnings.Add($"DATALINK_READWR: связей с незнакомой раскладкой: {undecoded} — адрес и данные ПЛК для них неизвестны");
        }
    }

    /// <summary>
    /// Прочитать таблицу DATALINK в <see cref="FwxDocument.AreaLinks"/>: её элементы устроены
    /// так же, как элементы DATALINK_READWR (наблюдение на A603A0097: указатели областей
    /// «Screen number», «Date/time PLC» — символьные пути в DB). Служебные элементы соединений
    /// другой раскладки остаются неразобранными без предупреждения: на них не ссылаются теги.
    /// Нет таблицы — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или короче общей части.</exception>
    public static void ParseAreaPointers(FwxDocument doc) => ReadTable(doc, "DATALINK", doc.AreaLinks);

    /// <summary>Прочитать элементы таблицы с раскладкой DATALINK_READWR в список, сразу после чтения каждого.</summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или короче общей части.</exception>
    private static void ReadTable(FwxDocument doc, string name, List<PlcLink> links)
    {
        var table = doc.FindTable(name);
        if (table is null)
        {
            return;
        }
        // цикл, а не AddRange: каждая связь попадает в список сразу, частичный результат остаётся при сбое
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            links.Add(ReadRecord(doc.Binary, item));
        }
    }

    /// <summary>
    /// Один элемент. Общая часть: <c>+0x10</c> цикл опроса в мс, <c>+0x17</c> номер
    /// соединения, <c>+0x1c</c> режим доступа (0 — символьный, 1 — абсолютный), <c>+0x1d</c>
    /// код типа ПЛК. Абсолютный (0x30 байт) и символьный (0x34 + 4·K байт) — см.
    /// <see cref="ReadAbsolute"/> и <see cref="ReadSymbolic"/>. Другая длина — элемент не разобран.
    /// </summary>
    /// <returns>Связь с ПЛК.</returns>
    /// <exception cref="FwxFormatException">Элемент короче общей части.</exception>
    private static PlcLink ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        if (item.Length < CommonSize)
        {
            // Section выставлен обходом таблицы: DATALINK_READWR или DATALINK
            throw new FwxFormatException(b.Section, p, $"элемент {item.Index + 1}: длина 0x{item.Length:x} меньше общей части");
        }

        var common = new PlcLink(item.Index, b.D1(p + 0x17), b.D4(p + 0x10), b.D1(p + 0x1c) == 1, b.D1(p + 0x1d),
            null, 0, 0, 0, 0, 0, [], 0, 0, p, false);
        if (common.Absolute && item.Length == AbsoluteSize)
        {
            return ReadAbsolute(b, common);
        }
        var levels = b.D1(p + 0x1f);
        if (!common.Absolute && item.Length == SymbolicBaseSize + levels * 4L)
        {
            return ReadSymbolic(b, common, levels);
        }
        return common;
    }

    /// <summary>
    /// Абсолютный доступ: <c>+0x1e</c> область (0 — DB), <c>+0x1f</c> бит, <c>+0x20</c> байт,
    /// <c>+0x24</c> номер DB, <c>+0x28</c> размер в битах, <c>+0x2a</c> число элементов.
    /// </summary>
    /// <returns>Связь; не разобрана, если область незнакома.</returns>
    private static PlcLink ReadAbsolute(FwxBinary b, PlcLink common)
    {
        var p = common.Offset;
        var area = PlcAreaCodes.FromAbsolute(b.D1(p + 0x1e));
        return common with
        {
            Area = area,
            DbNumber = area == PlcArea.DataBlock ? b.D2(p + 0x24) : 0,
            Bit = b.D1(p + 0x1f),
            ByteOffset = b.D4(p + 0x20),
            BitSize = b.D2(p + 0x28),
            Elements = b.D2(p + 0x2a),
            Decoded = area is not null
        };
    }

    /// <summary>
    /// Символьный доступ: <c>+0x1f</c> K — число уровней пути, <c>+0x20</c> область (0x50..0x54
    /// или 0x8a0e0000 + номер DB), <c>+0x24</c> хэш, <c>+0x28</c> K слов пути, за ними размер в
    /// битах (+2) и число элементов (+4). У I/Q/M/C/T путь из одного ID символа с флагом 0x40000000.
    /// </summary>
    /// <returns>Связь; не разобрана, если область незнакома.</returns>
    private static PlcLink ReadSymbolic(FwxBinary b, PlcLink common, int levels)
    {
        var p = common.Offset;
        var code = b.D4(p + 0x20);
        var area = PlcAreaCodes.FromSymbolic(code);
        var path = Enumerable.Range(0, levels).Select(i => b.D4(p + 0x28 + i * 4L)).ToList();
        var after = p + 0x28 + levels * 4L;
        var symbolId = area is not null and not PlcArea.DataBlock && levels > 0 ? path[^1] & ~SymbolIdFlag : 0;
        return common with
        {
            Area = area,
            DbNumber = PlcAreaCodes.DbFromSymbolic(code),
            SymbolId = symbolId,
            Hash = b.D4(p + 0x24),
            Path = path,
            BitSize = b.D2(after + 2),
            Elements = b.D2(after + 4),
            Decoded = area is not null
        };
    }
}
