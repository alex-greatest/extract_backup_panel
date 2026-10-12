using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Теги и биты дискретных алармов: для аларма и назначения — номер тега в VAR и бит.</summary>
public sealed class AlarmTagIndex
{
    /// <summary>По (ID аларма, назначение) — номер тега в <see cref="FwxDocument.Tags"/> (или <c>null</c>, если тега нет) и бит.</summary>
    private readonly Dictionary<(long AlarmId, AlarmBitRole Role), (int? Tag, int Bit)> _bits = [];

    /// <summary>
    /// Собрать индекс: элемент AREAPTR связан с тегом, чья запись VAR ссылается на этот элемент
    /// (Id таблицы AREAPTR из TOC). Если у аларма несколько битов одного назначения, берётся
    /// первый в порядке файла.
    /// </summary>
    public AlarmTagIndex(FwxDocument doc)
    {
        var areaTable = doc.FindTable("AREAPTR")?.Id;
        var tagByElement = new Dictionary<int, int>();
        for (var i = 0; i < doc.Tags.Count; i++)
        {
            foreach (var reference in doc.Tags[i].References.Where(r => r.Table == areaTable))
            {
                tagByElement.TryAdd(reference.Index, i);
            }
        }
        foreach (var element in doc.AlarmBits)
        {
            int? tag = tagByElement.TryGetValue(element.Index, out var t) ? t : null;
            foreach (var bit in element.Bits)
            {
                _bits.TryAdd((bit.AlarmId, element.Role), (tag, bit.Bit));
            }
        }
    }

    /// <summary>Тег и бит аларма <paramref name="alarmId"/> для назначения <paramref name="role"/>.</summary>
    /// <returns>Номер тега в VAR (<c>null</c> — тег не найден) и бит; <c>null</c> — у аларма нет такого тега.</returns>
    public (int? Tag, int Bit)? Find(long alarmId, AlarmBitRole role) =>
        _bits.TryGetValue((alarmId, role), out var found) ? found : null;
}
