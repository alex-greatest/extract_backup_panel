using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор MSG_NUMHANDLER: классы дискретных алармов панели — ID класса и строка его Display name.</summary>
public static class MsgNumHandlerParser
{
    /// <summary>
    /// Прочитать таблицу MSG_NUMHANDLER и добавить классы в <see cref="FwxDocument.AlarmClasses"/>
    /// в порядке файла; раскладка — <see cref="AlarmHandlerLayout.ReadClasses"/>. Элемент один;
    /// если их несколько, классы всех элементов идут подряд. Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или его длина не сходится с числами в нём.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("MSG_NUMHANDLER");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            AlarmHandlerLayout.ReadClasses(doc.Binary, item, doc.AlarmClasses);
        }
    }
}
