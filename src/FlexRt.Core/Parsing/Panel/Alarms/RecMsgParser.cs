using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор RECMSG: какие классы алармов пишутся в журнал алармов (колонка Log класса в TIA).</summary>
public static class RecMsgParser
{
    /// <summary>
    /// Прочитать таблицу RECMSG и добавить элементы в <see cref="FwxDocument.AlarmLogClasses"/> в
    /// порядке каталога. Таблица есть только в панелях с журналом алармов; нет таблицы в TOC —
    /// ничего не делает.
    /// <see cref="FwxDocument.AlarmLogClassesComplete"/> становится <c>true</c>, только если таблицы нет или она
    /// прочитана целиком.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или его длина не сходится с числами в нём.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("RECMSG");
        if (table is null)
        {
            doc.AlarmLogClassesComplete = true;
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            doc.AlarmLogClasses.Add(ReadRecord(doc.Binary, item));
        }
        doc.AlarmLogClassesComplete = true;
    }

    /// <summary>
    /// Один элемент (сверено на шаговых копиях A603A0097, V21, один журнал): начало
    /// <see cref="ClassListHeader.Read"/> (u16 перед K — всегда 2), затем K u16 — ID классов,
    /// которые пишутся в журнал (Log у Warnings добавил ID 2), нули до кратности 4. Элемент
    /// кончается ровно там.
    /// </summary>
    /// <returns>Классы журнала.</returns>
    /// <exception cref="FwxFormatException">Длина элемента не сходится с числом ссылок и классов.</exception>
    private static AlarmLogClasses ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var (idsOffset, count) = ClassListHeader.Read(b, item);
        if ((idsOffset + count * 2L + 3) / 4 * 4 != item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {item.Index + 1}: длина {item.Length} не сходится с {count} классами");
        }
        var ids = Enumerable.Range(0, count).Select(i => b.D2(p + idsOffset + i * 2L)).ToList();
        return new AlarmLogClasses(item.Index, ids);
    }
}
