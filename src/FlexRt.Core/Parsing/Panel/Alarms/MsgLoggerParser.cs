using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор MSG_LOGGER: функции, привязанные к классам алармов (E-mail address класса в TIA).</summary>
public static class MsgLoggerParser
{
    /// <summary>Размер записи функции класса.</summary>
    private const int EntrySize = 8;

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
    /// Один элемент (сверено на шаговой копии A603A0097, V21, один адрес): начало
    /// <see cref="ClassListHeader.Read"/> (u16 перед K — всегда 1), затем K записей по 8 байт: u16
    /// ID класса, u16 Id таблицы функции (наблюдение: FUNC_SEND_EMAIL), u16 номер элемента в ней
    /// (гипотеза: встречен только 0) и u16 (всегда 0, не разобрано). Элемент кончается ровно там.
    /// Каждая запись сразу добавляется в <paramref name="functions"/>.
    /// </summary>
    /// <exception cref="FwxFormatException">Длина элемента не сходится с числом ссылок и записей.</exception>
    private static void ReadRecord(FwxBinary b, TableItem item, List<AlarmClassFunction> functions)
    {
        var p = item.Offset;
        var (entries, count) = ClassListHeader.Read(b, item);
        if (entries + count * (long)EntrySize != item.Length)
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
