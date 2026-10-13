using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Имя HMI-тега поля в тексте дискретного аларма по связи поля с ПЛК.</summary>
public sealed class AlarmFieldNames
{
    /// <summary>Id таблицы DATALINK из TOC: на её элементы ссылаются поля.</summary>
    private readonly int? _fieldTable;

    /// <summary>Элементы DATALINK по номеру.</summary>
    private readonly Dictionary<int, PlcLink> _fieldLinks;

    /// <summary>Имя HMI-тега по ключу связи с ПЛК (<see cref="Key"/>), первый тег в порядке VAR.</summary>
    private readonly Dictionary<string, string> _tagByLink = [];

    /// <summary>
    /// Собрать индекс: для каждого PLC-тега, связанного через DATALINK_READWR, — ключ его связи
    /// с ПЛК и имя.
    /// </summary>
    public AlarmFieldNames(FwxDocument doc)
    {
        _fieldTable = doc.FindTable("DATALINK")?.Id;
        _fieldLinks = ByIndex(doc.AreaLinks);
        var readWrite = doc.FindTable("DATALINK_READWR")?.Id;
        var links = ByIndex(doc.Links);
        foreach (var tag in doc.Tags.Where(t => t.LinkTable == readWrite))
        {
            if (tag.LinkIndex is { } index && links.TryGetValue(index, out var link) && link.Decoded)
            {
                _tagByLink.TryAdd(Key(link), tag.Name);
            }
        }
    }

    /// <summary>Связи по номеру элемента; при повторе номера — первая в порядке списка.</summary>
    /// <returns>Словарь «номер элемента — связь».</returns>
    private static Dictionary<int, PlcLink> ByIndex(IEnumerable<PlcLink> links) =>
        links.GroupBy(l => l.Index).ToDictionary(g => g.Key, g => g.First());

    /// <summary>
    /// Имя HMI-тега поля: панель хранит у поля не тег, а отдельную связь с ПЛК в DATALINK; её
    /// адрес или символьный путь совпадает со связью HMI-тега в DATALINK_READWR (наблюдение на
    /// A603A0097: поле аларма 30 → <c>DB_ST7_Axis_Axis_Receive_FAULT_CODE</c>, как в TIA).
    /// </summary>
    /// <returns>Имя тега или <c>?</c>, если связь не найдена.</returns>
    public string Find(AlarmField field)
    {
        if (field.SourceTable != _fieldTable || !_fieldLinks.TryGetValue(field.SourceIndex, out var link) || !link.Decoded)
        {
            return "?";
        }
        return _tagByLink.GetValueOrDefault(Key(link), "?");
    }

    /// <summary>Ключ связи с ПЛК: соединение, вид доступа, область, DB, адрес и символьный путь.</summary>
    /// <returns>Ключ.</returns>
    private static string Key(PlcLink link) =>
        $"{link.Connection}|{link.Absolute}|{link.Area}|{link.DbNumber}|{link.ByteOffset}|{link.Bit}|{string.Join(",", link.Path)}";
}
