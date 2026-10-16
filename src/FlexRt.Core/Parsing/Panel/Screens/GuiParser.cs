using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Разбор GUI: номер стартового экрана панели.</summary>
public static class GuiParser
{
    /// <summary>
    /// Смещение u16 номера стартового экрана от конца ссылок (+0x34 от начала элемента при трёх ссылках).
    /// Поля между ссылками и номером не разобраны.
    /// </summary>
    private const int StartScreenOffset = 0x14;

    /// <summary>
    /// Прочитать первый элемент таблицы GUI (в файлах он один) и записать номер стартового экрана в
    /// <see cref="FwxDocument.StartScreenNumber"/>. Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или не сходится с раскладкой.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("GUI");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table).Take(1))
        {
            doc.StartScreenNumber = ReadStartScreen(doc.Binary, item);
        }
    }

    /// <summary>
    /// Номер стартового экрана (сверено со Start screen в Runtime settings TIA на шаговых копиях Project1,
    /// V21): начало со ссылками (<see cref="ReferenceHeader"/>), за ними на <see cref="StartScreenOffset"/> — u16.
    /// </summary>
    /// <returns>Номер стартового экрана (Screen number в TIA).</returns>
    /// <exception cref="FwxFormatException">Начало элемента не сходится или номер не помещается в элемент.</exception>
    private static int ReadStartScreen(FwxBinary b, TableItem item)
    {
        var (end, _) = ReferenceHeader.Read(b, item);
        var offset = end + StartScreenOffset;
        return offset + 2 <= item.Length
            ? b.D2(item.Offset + offset)
            : throw new FwxFormatException(b.Section, item.Offset, $"запись {item.Index + 1}: номер стартового экрана не помещается в {item.Length} байт");
    }
}
