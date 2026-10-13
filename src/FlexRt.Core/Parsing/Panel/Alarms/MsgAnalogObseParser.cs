using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор MSG_ANALOG_OBSE: пределы аналоговых алармов — значение или тег предела и Limit mode.</summary>
public static class MsgAnalogObseParser
{
    /// <summary>Начало ссылок: за u32 (наблюдение: всегда 1) и u32 числом ссылок.</summary>
    private const int ReferencesOffset = 8;

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова, как ссылки записи VAR.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Размер полей после ссылок: u16 ID, u8 Limit mode, u8 вид значения, u32 значение.</summary>
    private const int BodySize = 8;

    /// <summary>Наибольшее число ссылок, которое принимается без ошибки (наблюдение: 1 и 2).</summary>
    private const long MaxReferences = 0x20;

    /// <summary>
    /// Прочитать таблицу MSG_ANALOG_OBSE и добавить пределы в <see cref="FwxDocument.AnalogLimits"/>
    /// в порядке файла. Ссылка на элемент таблицы VAR (Id из TOC) — тег предела. Таблица есть только
    /// в панелях с аналоговыми алармами; нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или его длина не сходится с числом ссылок.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("MSG_ANALOG_OBSE");
        if (table is null)
        {
            return;
        }

        var varTable = doc.FindTable("VAR")?.Id;
        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            doc.AnalogLimits.Add(ReadRecord(doc.Binary, item, varTable));
        }
    }

    /// <summary>
    /// Один элемент (сверено на шаговых копиях A603A0097, 4 аларма): <c>+0x00</c> u32 (всегда 1,
    /// не разобрано), <c>+0x04</c> u32 R — число ссылок, R ссылок по 8 байт в разном порядке
    /// (наблюдение: на MSG_ANALOGHANDL со словами <c>0x4e</c>, <c>0x8000</c> — всегда; на запись
    /// VAR со словами 0, <c>0x1a</c> — тег предела), затем u16 ID аларма, u8 Limit mode, u8 вид
    /// значения и u32 значение предела. Элемент кончается ровно там. Если ссылок на VAR несколько,
    /// берётся первая.
    /// </summary>
    /// <returns>Предел аларма.</returns>
    /// <exception cref="FwxFormatException">Длина элемента не сходится с числом ссылок.</exception>
    private static AnalogAlarmLimit ReadRecord(FwxBinary b, TableItem item, int? varTable)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < ReferencesOffset + BodySize)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 0x{ReferencesOffset + BodySize:x}");
        }
        var references = b.D4(p + 4);
        if (references > MaxReferences || ReferencesOffset + references * ReferenceSize + BodySize != item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} не сходится с {references} ссылками");
        }
        var body = p + ReferencesOffset + references * ReferenceSize;
        var limitTag = FindLimitTag(b, p + ReferencesOffset, (int)references, varTable);
        return new AnalogAlarmLimit(item.Index, b.D2(body), b.D1(body + 2), b.D1(body + 3), b.D4(body + 4), limitTag);
    }

    /// <summary>
    /// Тег предела: номер элемента (u16 на +2) в первой из <paramref name="count"/> ссылок,
    /// начиная с <paramref name="start"/>, чей Id таблицы (u16 на +0) — <paramref name="varTable"/>.
    /// </summary>
    /// <returns>Номер записи VAR; ссылки на VAR нет — <c>null</c>.</returns>
    private static int? FindLimitTag(FwxBinary b, long start, int count, int? varTable) =>
        Enumerable.Range(0, count)
            .Select(i => start + i * ReferenceSize)
            .Where(reference => b.D2(reference) == varTable)
            .Select(reference => (int?)b.D2(reference + 2))
            .FirstOrDefault();
}
