using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор FUNC_SEND_EMAIL: адреса писем классов алармов (E-mail address класса в TIA).</summary>
public static class FuncSendEmailParser
{
    /// <summary>
    /// Смещение u32 длины адреса в байтах; за ним — адрес UTF-16LE. Наблюдение на 4 элементах (адреса
    /// от 10 до 21 символа): до него — u32 1, 0x0c байт и четыре блока по 0x10 байт (не разобраны,
    /// размер не зависит от длины адреса).
    /// </summary>
    private const int AddressLengthOffset = 0x50;

    /// <summary>
    /// Прочитать таблицу FUNC_SEND_EMAIL и добавить адреса в <see cref="FwxDocument.EmailFunctions"/>
    /// в порядке каталога. Таблица есть только в панелях, где у класса задан E-mail address; нет
    /// таблицы в TOC — ничего не делает.
    /// <see cref="FwxDocument.EmailFunctionsComplete"/> становится <c>true</c>, только если таблицы нет или она
    /// прочитана целиком.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или адрес не помещается в элемент.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("FUNC_SEND_EMAIL");
        if (table is null)
        {
            doc.EmailFunctionsComplete = true;
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            doc.EmailFunctions.Add(ReadRecord(doc.Binary, item));
        }
        doc.EmailFunctionsComplete = true;
    }

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
