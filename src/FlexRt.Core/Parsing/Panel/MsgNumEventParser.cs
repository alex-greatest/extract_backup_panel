using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор MSG_NUMEVENT: дискретные алармы панели — ID, текст, класс, Report и поля в тексте.</summary>
public static class MsgNumEventParser
{
    /// <summary>
    /// Прочитать таблицу MSG_NUMEVENT и добавить алармы в <see cref="FwxDocument.DiscreteAlarms"/>
    /// в порядке файла; раскладка элемента — <see cref="AlarmMessageLayout.Read"/>. Нет таблицы в
    /// TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или его раскладка не сходится.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("MSG_NUMEVENT");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            var (id, textIdx, classIndex, report, fields) = AlarmMessageLayout.Read(doc.Binary, item);
            doc.DiscreteAlarms.Add(new DiscreteAlarm(id, textIdx, classIndex, report, fields));
        }
    }
}
