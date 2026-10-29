using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор FUNC_SEND_EMAIL: адреса писем классов алармов (E-mail address класса в TIA), только элементы со ссылкой из MSG_LOGGER.</summary>
public static class FuncSendEmailParser
{
    /// <summary>
    /// Смещение u32 длины адреса в байтах; за ним — адрес UTF-16LE. Наблюдение на 4 элементах (адреса
    /// от 10 до 21 символа): до него — u32 1, 0x0c байт и четыре блока по 0x10 байт (не разобраны,
    /// размер не зависит от длины адреса).
    /// </summary>
    private const int AddressLengthOffset = 0x50;

    /// <summary>
    /// Прочитать элементы таблицы FUNC_SEND_EMAIL, на которые ссылаются классы алармов
    /// (<see cref="FwxDocument.AlarmClassFunctions"/> с Id этой таблицы), и добавить их адреса в
    /// <see cref="FwxDocument.EmailFunctions"/> в порядке каталога. В ту же таблицу TIA пишет и системную
    /// функцию SendEMail из событий экрана (наблюдение на steps-events, V21); такие элементы не читаются.
    /// Ссылка на номер, которого в таблице нет, пропускается: у класса адреса нет. Таблица есть только в
    /// панелях, где у класса задан E-mail address или вызвана SendEMail; нет таблицы в TOC — ничего не делает.
    /// <see cref="FwxDocument.EmailFunctionsComplete"/> становится <c>true</c>, только если таблицы нет или все
    /// элементы со ссылками прочитаны.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент со ссылкой выходит за пределы таблицы или адрес не помещается в элемент.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("FUNC_SEND_EMAIL");
        if (table is null)
        {
            doc.EmailFunctionsComplete = true;
            return;
        }

        foreach (var index in ReferencedIndexes(doc, table))
        {
            doc.EmailFunctions.Add(ReadRecord(doc.Binary, FwxReader.CheckedItem(doc.Binary, table, index)));
        }
        doc.EmailFunctionsComplete = true;
    }

    /// <summary>
    /// Номера элементов FUNC_SEND_EMAIL, на которые ссылаются записи MSG_LOGGER (функции классов алармов),
    /// без повторов и номеров за концом каталога, по возрастанию. MSG_LOGGER разобрана раньше
    /// (<see cref="FwxReader.Read"/>); если она прочитана не целиком, берутся ссылки до сбоя.
    /// </summary>
    /// <returns>Номера элементов с нуля.</returns>
    private static IEnumerable<int> ReferencedIndexes(FwxDocument doc, TocEntry table) =>
        doc.AlarmClassFunctions
            .Where(f => f.Table == table.Id && f.Index < table.Entries)
            .Select(f => f.Index)
            .Distinct()
            .Order();

    /// <summary>
    /// Адрес элемента (наблюдение на шаговых копиях A603A0097 и Project1, V21: 4 адреса совпали с
    /// TIA): на <see cref="AddressLengthOffset"/> u32 длина в байтах, за ней адрес UTF-16LE. Блоки до
    /// него не разобраны; при другой их раскладке адрес окажется в другом месте.
    /// </summary>
    /// <returns>Адрес письма.</returns>
    /// <exception cref="FwxFormatException">Длина нечётна или адрес не помещается в элемент.</exception>
    private static EmailFunction ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < AddressLengthOffset + 4)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 0x{AddressLengthOffset + 4:x}");
        }
        var bytes = b.D4(p + AddressLengthOffset);
        if (bytes % 2 != 0 || AddressLengthOffset + 4 + bytes > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: адрес из {bytes} байт не помещается в {item.Length} байт");
        }
        return new EmailFunction(item.Index, b.GetNameLen(p + AddressLengthOffset + 4, (int)(bytes / 2)));
    }
}
