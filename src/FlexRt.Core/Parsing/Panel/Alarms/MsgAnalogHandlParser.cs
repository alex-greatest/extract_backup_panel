using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор MSG_ANALOGHANDL: классы аналоговых алармов панели — ID класса и строка его Display name.</summary>
public static class MsgAnalogHandlParser
{
    /// <summary>
    /// Прочитать таблицу MSG_ANALOGHANDL и добавить классы в
    /// <see cref="FwxDocument.AnalogAlarmClasses"/> в порядке файла; раскладка та же, что у
    /// MSG_NUMHANDLER (<see cref="AlarmHandlerLayout.ReadClasses"/>; сверено на шаговых копиях
    /// A603A0097 с 0, 1 и 4 аналоговыми алармами). Таблица есть и в панелях без аналоговых алармов
    /// (N = 0). Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или его длина не сходится с числами в нём.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("MSG_ANALOGHANDL");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            AlarmHandlerLayout.ReadClasses(doc.Binary, item, doc.AnalogAlarmClasses);
        }
    }
}
