using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>
/// Разбор SECURITY_UNIT: группы пользователей. Сверено с TIA на шаговых копиях Project1 (V21, TP700 Comfort,
/// <c>data/real/steps-iofield/</c> 68 — третья группа, 69 — её номер 5 и только Monitor) и на A603A0097 (номера и русские
/// Display name двух стандартных групп — со снимком TIA); <c>samples/pdata.fwc</c> с TIA не сверялся.
/// </summary>
public static class UserGroupParser
{
    /// <summary>
    /// u32 число групп + 1 от конца ссылок (гипотеза по двум шагам: 3 при двух группах, 4 при трёх — и при номерах 1–3,
    /// и при номерах 1, 2, 5); только сверяется с числом записей. Перед ним 8 нулевых байт и 4 байта (01, 03 или FF, 03, 00) — не читаются.
    /// </summary>
    private const int CountOffset = 0x0c;

    /// <summary>
    /// Начало записей групп от конца ссылок; перед ним 8 нулевых байт и u32 (наблюдение: всегда 0x3F7B7) — не читаются.
    /// </summary>
    private const int GroupsOffset = 0x1c;

    /// <summary>
    /// Размер записи группы: u16 Number, u16 (наблюдение: всегда 1), u32 маска прав, u32 номер строки Display name.
    /// </summary>
    private const int GroupSize = 12;

    /// <summary>Маска прав от начала записи.</summary>
    private const int RightsOffset = 4;

    /// <summary>Номер строки Display name от начала записи.</summary>
    private const int TextOffset = 8;

    /// <summary>
    /// Прочитать группы первого элемента SECURITY_UNIT в <see cref="FwxDocument.UserGroups"/> (в файлах элемент один).
    /// Нет таблицы или элементов — групп нет, без сообщения.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент вне таблицы или группы не сходятся с длиной элемента.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("SECURITY_UNIT");
        if (table is null || table.Entries == 0)
        {
            return;
        }
        ReadRecord(doc, FwxReader.CheckedItem(doc.Binary, table, 0));
    }

    /// <summary>
    /// Элемент: u32 1, u32 R, R ссылок (в файлах одна — на GUI), за ними поля и записи групп (<see cref="GroupSize"/>)
    /// до конца элемента. Группы читаются по длине элемента — все целые записи, каждая сразу добавляется в документ; число
    /// групп в заголовке (смысл — наблюдение по двум шагам) только сверяется с ними.
    /// </summary>
    /// <exception cref="FwxFormatException">
    /// Начало элемента не сходится, после записей остался хвост короче записи или число в заголовке не сходится с числом
    /// записей (группы при этом прочитаны).
    /// </exception>
    private static void ReadRecord(FwxDocument doc, TableItem item)
    {
        var b = doc.Binary;
        var (end, _) = ReferenceHeader.Read(b, item);
        var first = end + GroupsOffset;
        if (first > item.Length)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {item.Index + 1}: группы не помещаются в {item.Length} байт");
        }
        var count = (item.Length - first) / GroupSize;
        for (var i = 0; i < count; i++)
        {
            var p = item.Offset + first + i * GroupSize;
            doc.UserGroups.Add(new UserGroup(b.D2(p), b.D4(p + RightsOffset), b.D4(p + TextOffset)));
        }
        var stated = b.D4(item.Offset + end + CountOffset) - 1;
        if (stated != count || (item.Length - first) % GroupSize != 0)
        {
            throw new FwxFormatException(
                b.Section,
                item.Offset,
                $"запись {item.Index + 1}: групп по заголовку {stated}, а записей по {GroupSize} байт в {item.Length - first} байт — {count}");
        }
    }
}
