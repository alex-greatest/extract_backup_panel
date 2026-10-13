using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор MSG_ANALOGCREAT: аналоговые алармы панели — ID, текст, класс, Report и поля в тексте.</summary>
public static class MsgAnalogCreatParser
{
    /// <summary>
    /// Прочитать таблицу MSG_ANALOGCREAT и добавить алармы в <see cref="FwxDocument.AnalogAlarms"/>
    /// в порядке файла. Раскладка элемента та же, что у MSG_NUMEVENT
    /// (<see cref="AlarmMessageLayout.Read"/>; сверено на шаговых копиях A603A0097: ID, текст,
    /// класс, Report, одно поле тега); номер класса — в списке классов MSG_ANALOGHANDL. Таблица есть
    /// только в панелях с аналоговыми алармами; нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или его раскладка не сходится.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("MSG_ANALOGCREAT");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            var (id, textIdx, classIndex, report, fields) = AlarmMessageLayout.Read(doc.Binary, item);
            doc.AnalogAlarms.Add(new AnalogAlarm(id, textIdx, classIndex, report, fields));
        }
    }
}
