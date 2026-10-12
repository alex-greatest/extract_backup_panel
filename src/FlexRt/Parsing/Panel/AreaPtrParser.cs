using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор AREAPTR: теги срабатывания и квитирования дискретных алармов — пары «аларм — бит».</summary>
public static class AreaPtrParser
{
    /// <summary>Вид элемента (u32 сразу за именем): тег срабатывания или квитирования из ПЛК.</summary>
    private const int KindTagBits = 1;

    /// <summary>Вид элемента: тег квитирования с панели (раскладка на 8 байт длиннее).</summary>
    private const int KindHmiAcknowledgment = 2;

    /// <summary>На сколько байт поля вида 2 (назначение, число пар, пары) сдвинуты относительно вида 1.</summary>
    private const int HmiAcknowledgmentShift = 8;

    /// <summary>Смещение кода назначения (u16) от поля вида у вида 1.</summary>
    private const int PurposeOffset = 0x08;

    /// <summary>Смещение числа пар (u16) от поля вида у вида 1.</summary>
    private const int CountOffset = 0x10;

    /// <summary>
    /// От числа пар до первой пары: u16 число пар, u16 <c>0x0a</c> (бывает <c>0x8a</c>), u16 номер
    /// элемента с единицы, u16 9 — наблюдение.
    /// </summary>
    private const int CountToPairs = 8;

    /// <summary>Размер пары: u16 ID аларма и u16 бит.</summary>
    private const long PairSize = 4;

    /// <summary>Код назначения (u16) у тега срабатывания.</summary>
    private const int PurposeTrigger = 0x4e;

    /// <summary>Код назначения (u16) у тега квитирования (из ПЛК и с панели).</summary>
    private const int PurposeAcknowledgment = 0x4f;

    /// <summary>
    /// Прочитать таблицу AREAPTR и добавить элементы в <see cref="FwxDocument.AlarmBits"/> в
    /// порядке каталога. Элемент незнакомой раскладки пропускается, разбор продолжается; после
    /// таблицы в <see cref="FwxDocument.Warnings"/> — одно сообщение с числом пропущенных и первой
    /// причиной. <see cref="FwxDocument.AlarmBitsComplete"/> становится <c>true</c>, только если
    /// таблицы нет в TOC или прочитаны все элементы: при пропуске или ошибке остаётся <c>false</c>.
    /// </summary>
    /// <exception cref="FwxFormatException">Каталог смещений испорчен или элемент выходит за пределы таблицы.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("AREAPTR");
        if (table is null)
        {
            doc.AlarmBitsComplete = true;
            return;
        }

        var skipped = new List<string>();
        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            var bits = ReadRecord(doc.Binary, item, out var reason);
            if (bits is null)
            {
                skipped.Add(reason);
                continue;
            }
            doc.AlarmBits.Add(bits);
        }
        if (skipped.Count > 0)
        {
            doc.Warnings.Add($"AREAPTR: пропущено элементов с незнакомой раскладкой: {skipped.Count}, первый — {skipped[0]}");
            return;
        }
        doc.AlarmBitsComplete = true;
    }

    /// <summary>
    /// Один элемент: <c>+0x00</c> u16 1, <c>+0x02</c> длина имени в символах, имя UTF-16LE
    /// (наблюдение: номер элемента с единицы, <c>1</c>, <c>22</c>), нули до кратности 4; за ними
    /// u32 вид. Вид 1: <c>+0x08</c> u16 назначение (<see cref="PurposeTrigger"/> — срабатывание,
    /// <see cref="PurposeAcknowledgment"/> — квитирование из ПЛК), <c>+0x0c</c> размер тега в
    /// битах, <c>+0x10</c> u16 число пар, пары с <c>+0x18</c>. Вид 2 — квитирование с панели:
    /// назначение на <c>+0x10</c>, число пар на <c>+0x18</c>, пары с <c>+0x20</c>. Смещения — от
    /// поля вида. Пара — u16 ID аларма и u16 бит. Элемент кончается ровно на последней паре.
    /// Сверено с TIA на A603A0097 (все три назначения).
    /// </summary>
    /// <returns>Элемент или <c>null</c>, если раскладка незнакома (причина — в <paramref name="reason"/>).</returns>
    private static AlarmBits? ReadRecord(FwxBinary b, TableItem item, out string reason)
    {
        reason = $"элемент {item.Index + 1} на 0x{item.Offset:x}";
        var end = item.Offset + item.Length;
        if (item.Length < 4)
        {
            reason += $": длина {item.Length} меньше заголовка имени";
            return null;
        }
        var nameEnd = 4L + b.D2(item.Offset + 2) * 2;
        var p = item.Offset + (nameEnd + 3) / 4 * 4;
        if (p + 4 > end)
        {
            reason += ": имя выходит за конец элемента";
            return null;
        }
        var kind = b.D4(p);
        var shift = kind == KindHmiAcknowledgment ? HmiAcknowledgmentShift : 0;
        var count = p + CountOffset + shift;
        if (kind is not (KindTagBits or KindHmiAcknowledgment) || count + CountToPairs > end)
        {
            reason += $": вид {kind}";
            return null;
        }
        var purpose = b.D2(p + PurposeOffset + shift);
        var role = Role(kind, purpose);
        var pairCount = b.D2(count);
        var pairs = count + CountToPairs;
        if (role is null || pairs + pairCount * PairSize != end)
        {
            reason += $": вид {kind}, назначение 0x{purpose:x}, пар {pairCount}";
            return null;
        }
        var bits = Enumerable.Range(0, pairCount)
            .Select(i => new AlarmBit(b.D2(pairs + i * PairSize), b.D2(pairs + i * PairSize + 2)))
            .ToList();
        return new AlarmBits(item.Index, role.Value, bits);
    }

    /// <summary>Назначение элемента по виду и коду назначения.</summary>
    /// <returns>Назначение или <c>null</c>, если сочетание незнакомо.</returns>
    private static AlarmBitRole? Role(long kind, int purpose) => (kind, purpose) switch
    {
        (KindTagBits, PurposeTrigger) => AlarmBitRole.Trigger,
        (KindTagBits, PurposeAcknowledgment) => AlarmBitRole.PlcAcknowledgment,
        (KindHmiAcknowledgment, PurposeAcknowledgment) => AlarmBitRole.HmiAcknowledgment,
        _ => null
    };
}
