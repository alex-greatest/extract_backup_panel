using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор CONNECTION_OMSP: соединения панели с ПЛК — имя и IP ПЛК.</summary>
public static class ConnectionParser
{
    /// <summary>
    /// Сдвиг IP ПЛК от конца имени, выровненного до 4 байт. Наблюдение на TIA V21 / S7-1500:
    /// после выравнивания идут <c>01 00 00 00 | 50 01 00 00 | 4c 01 4c 81 | NN 56 03 00 | 01 00</c>,
    /// затем 4 байта IP. Проверено на именах из 16 и 7 символов.
    /// </summary>
    private const int IpAfterName = 0x12;

    /// <summary>
    /// Прочитать таблицу CONNECTION_OMSP и добавить соединения в
    /// <see cref="FwxDocument.Connections"/> в порядке каталога: номер соединения у PLC-тега —
    /// индекс в этом списке. Нет таблицы в TOC — ничего не делает. Каждое соединение
    /// добавляется сразу после чтения.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или короче имени и IP.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("CONNECTION_OMSP");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            doc.Connections.Add(ReadRecord(doc.Binary, item));
        }
    }

    /// <summary>
    /// Один элемент: <c>+0x00</c> слово (наблюдение: всегда 3), <c>+0x02</c> длина имени в
    /// символах, <c>+0x04</c> имя UTF-16LE, нули до кратности 4, через <see cref="IpAfterName"/>
    /// байт — IP ПЛК, первая часть адреса первым байтом (<c>c0 a8 00 01</c> = 192.168.0.1).
    /// </summary>
    /// <returns>Соединение.</returns>
    /// <exception cref="FwxFormatException">Имя и IP не помещаются в элемент.</exception>
    private static HmiConnection ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var chars = b.D2(p + 2);
        var ipPos = (4L + chars * 2 + 3) / 4 * 4 + IpAfterName;
        if (ipPos + 4 > item.Length)
        {
            throw new FwxFormatException("CONNECTION_OMSP", p,
                $"элемент {item.Index + 1}: длина 0x{item.Length:x} меньше имени из {chars} символов и IP");
        }

        var name = b.GetNameLen(p + 4, chars);
        return new HmiConnection(item.Index, name, b.D4BigEndian(p + ipPos));
    }
}
