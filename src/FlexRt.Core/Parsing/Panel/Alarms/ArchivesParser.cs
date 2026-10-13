using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

/// <summary>Разбор ARCHIVES: имена журналов панели и их ссылки (журнал алармов ссылается на элемент RECMSG).</summary>
public static class ArchivesParser
{
    /// <summary>Первое слово элемента (наблюдение: всегда 3, как у записи VAR).</summary>
    private const int RecordMarker = 3;

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова.</summary>
    private const int ReferenceSize = 8;

    /// <summary>
    /// Прочитать таблицу ARCHIVES и добавить журналы в <see cref="FwxDocument.Archives"/> в порядке
    /// каталога. Нет таблицы в TOC — ничего не делает.
    /// <see cref="FwxDocument.ArchivesComplete"/> становится <c>true</c>, только если таблицы нет или она
    /// прочитана целиком.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или не сходится с раскладкой.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("ARCHIVES");
        if (table is null)
        {
            doc.ArchivesComplete = true;
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            doc.Archives.Add(ReadRecord(doc.Binary, item));
        }
        doc.ArchivesComplete = true;
    }

    /// <summary>
    /// Начало элемента (наблюдение на A603A0097, V21: два журнала данных и журнал алармов): u16 3,
    /// u16 длина имени в символах, имя UTF-16LE, нули до кратности 4, u32 R — число ссылок, R ссылок
    /// по 8 байт (u16 Id таблицы, u16 номер элемента, два слова; у журнала алармов — одна, на
    /// элемент RECMSG; у журнала данных — по одной на тег, поэтому число не ограничивается). Байты
    /// после ссылок не читаются.
    /// </summary>
    /// <returns>Журнал.</returns>
    /// <exception cref="FwxFormatException">Элемент короче 4 байт, первое слово не 3 или имя и ссылки не помещаются в элемент.</exception>
    private static PanelArchive ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < 4)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 4");
        }
        if (b.D2(p) != RecordMarker)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: первое слово не 3");
        }
        var chars = b.D2(p + 2);
        var countOffset = (4 + chars * 2L + 3) / 4 * 4;
        if (countOffset + 4 > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: имя из {chars} символов не помещается в {item.Length} байт");
        }
        var references = b.D4(p + countOffset);
        var first = countOffset + 4;
        if (first + references * ReferenceSize > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: {references} ссылок не помещаются в {item.Length} байт");
        }
        var list = Enumerable.Range(0, (int)references)
            .Select(i => p + first + i * ReferenceSize)
            .Select(r => new VarReference(b.D2(r), b.D2(r + 2)))
            .ToList();
        return new PanelArchive(item.Index, b.GetNameLen(p + 4, chars), list);
    }
}
