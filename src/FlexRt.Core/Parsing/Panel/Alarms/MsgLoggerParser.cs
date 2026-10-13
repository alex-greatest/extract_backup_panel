using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор MSG_LOGGER: функции, привязанные к классам алармов (E-mail address класса в TIA).</summary>
public static class MsgLoggerParser
{
    /// <summary>Размер записи функции класса: u16 ID класса, u16 Id таблицы, u16 номер элемента.</summary>
    private const int EntrySize = 6;

    /// <summary>
    /// Прочитать таблицу MSG_LOGGER и добавить записи в <see cref="FwxDocument.AlarmClassFunctions"/>
    /// в порядке файла. Нет таблицы в TOC — ничего не делает.
    /// <see cref="FwxDocument.AlarmClassFunctionsComplete"/> становится <c>true</c>, только если таблицы нет или она
    /// прочитана целиком.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или его длина не сходится с числами в нём.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("MSG_LOGGER");
        if (table is null)
        {
            doc.AlarmClassFunctionsComplete = true;
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            ReadRecord(doc.Binary, item, doc.AlarmClassFunctions);
        }
        doc.AlarmClassFunctionsComplete = true;
    }

    /// <summary>
    /// Один элемент (сверено на шаговых копиях A603A0097 — один адрес — и Project1 — два адреса у
    /// двух классов, V21): начало <see cref="ClassListHeader.Read"/> (u16 перед K — всегда 1), затем
    /// K записей по 6 байт: u16 ID класса, u16 Id таблицы функции (наблюдение: FUNC_SEND_EMAIL) и
    /// u16 номер элемента в ней (0 и 1 у двух адресов), нули до кратности 4. Элемент кончается ровно там.
    /// Каждая запись сразу добавляется в <paramref name="functions"/>.
    /// </summary>
    /// <exception cref="FwxFormatException">Длина элемента не сходится с числом ссылок и записей.</exception>
    private static void ReadRecord(FwxBinary b, TableItem item, List<AlarmClassFunction> functions)
    {
        var p = item.Offset;
        var (entries, count) = ClassListHeader.Read(b, item);
        if ((entries + count * (long)EntrySize + 3) / 4 * 4 != item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {item.Index + 1}: длина {item.Length} не сходится с {count} записями");
        }
        for (var i = 0; i < count; i++)
        {
            var e = p + entries + i * (long)EntrySize;
            functions.Add(new AlarmClassFunction(b.D2(e), b.D2(e + 2), b.D2(e + 4)));
        }
    }
}
