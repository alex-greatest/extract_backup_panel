using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор MSG_NUMEVENT: дискретные алармы панели — ID, текст, класс, Report и поля в тексте.</summary>
public static class MsgNumEventParser
{
    /// <summary>Начало ссылок полей: за u32 0 и u32 числом ссылок.</summary>
    private const int ReferencesOffset = 8;

    /// <summary>Размер ссылки: Id таблицы, номер элемента и два слова, как ссылки записи VAR.</summary>
    private const int ReferenceSize = 8;

    /// <summary>На поле — две ссылки: на связь тега поля с ПЛК и вторая (не разобрана).</summary>
    private const int ReferencesPerField = 2;

    /// <summary>Размер полей после ссылок: строка текста, вторая строка, ID, класс и флаги.</summary>
    private const int BodySize = 0xc;

    /// <summary>Наибольшее число ссылок, которое принимается без ошибки (наблюдение: 0, 2, 4).</summary>
    private const long MaxReferences = 0x20;

    /// <summary>Младшие биты байта класса — номер класса; старшие три — число полей.</summary>
    private const int ClassIndexMask = 0x1f;

    /// <summary>Сдвиг числа полей в байте класса.</summary>
    private const int FieldCountShift = 5;

    /// <summary>Бит Report = On в байте флагов; сверено с TIA на A603A0097 (аларм 1, только эта правка).</summary>
    private const int ReportFlag = 0x40;

    /// <summary>Бит поля текстового списка в коде поля; без него — поле тега.</summary>
    private const int TextListFlag = 0x8000;

    /// <summary>
    /// Прочитать таблицу MSG_NUMEVENT и добавить алармы в <see cref="FwxDocument.DiscreteAlarms"/>
    /// в порядке файла. Нет таблицы в TOC — ничего не делает.
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
            doc.DiscreteAlarms.Add(ReadRecord(doc.Binary, item));
        }
    }

    /// <summary>
    /// Один элемент: <c>+0x00</c> u32 0, <c>+0x04</c> u32 R — число ссылок (по две на поле), R
    /// ссылок по 8 байт, затем u32 строка текста, u32 вторая строка (наблюдение: имя соединения
    /// панели, <c>HMI_ST7</c>, <c>HMI1</c>; не выводится), u16 ID, u8 класс (младшие 5 бит — номер
    /// класса, старшие 3 — число полей), u8 флаги (<see cref="ReportFlag"/>), по u16 коду на поле и
    /// нули до кратности 4. Элемент кончается ровно там.
    /// </summary>
    /// <returns>Аларм.</returns>
    /// <exception cref="FwxFormatException">Длина элемента, число ссылок или число полей не сходятся.</exception>
    private static DiscreteAlarm ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < ReferencesOffset + BodySize)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 0x{ReferencesOffset + BodySize:x}");
        }
        var references = b.D4(p + 4);
        var fieldCount = references / ReferencesPerField;
        var body = p + ReferencesOffset + references * ReferenceSize;
        var codesSize = (fieldCount * 2 + 3) / 4 * 4;
        if (references > MaxReferences || references % ReferencesPerField != 0
            || body + BodySize + codesSize != p + item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} не сходится с {references} ссылками");
        }
        var classByte = b.D1(body + 0xa);
        var declaredFields = classByte >> FieldCountShift;
        if (declaredFields != fieldCount)
        {
            throw new FwxFormatException(b.Section, body + 0xa, $"запись {number}: в байте класса {declaredFields} полей, а ссылок на {fieldCount}");
        }
        var textIdx = b.D4(body);
        var id = b.D2(body + 8);
        var report = (b.D1(body + 0xb) & ReportFlag) != 0;
        var fields = Enumerable.Range(0, (int)fieldCount)
            .Select(i => ReadField(b, p + ReferencesOffset + i * ReferencesPerField * ReferenceSize, b.D2(body + BodySize + i * 2L)))
            .ToList();
        return new DiscreteAlarm(id, textIdx, classByte & ClassIndexMask, report, fields);
    }

    /// <summary>
    /// Поле: первая из двух ссылок — u16 Id таблицы и u16 номер элемента связи тега поля с ПЛК
    /// (наблюдение: таблица DATALINK; у поля текстового списка бывает 0); код поля — бит
    /// <see cref="TextListFlag"/> и младший байт: длина поля тега (сверено на одном поле: 6) или
    /// номер текстового списка (гипотеза).
    /// </summary>
    /// <returns>Поле.</returns>
    private static AlarmField ReadField(FwxBinary b, long reference, int code) =>
        new((code & TextListFlag) != 0, code & 0xff, b.D2(reference), b.D2(reference + 2));
}
