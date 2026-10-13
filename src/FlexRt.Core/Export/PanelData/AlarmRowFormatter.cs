using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Колонки листа «Дискретные алармы» после текстов: класс, теги, биты, адреса и Report.</summary>
public static class AlarmRowFormatter
{
    /// <summary>Тег не задан — как в TIA.</summary>
    private const string NoTag = "<No tag>";

    /// <summary>Текст ячейки адреса, если тег не найден среди тегов панели или AREAPTR прочитана не целиком.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>
    /// Значения колонок Alarm class, Trigger tag, Trigger bit, Trigger address, HMI
    /// acknowledgment tag, bit, address, PLC acknowledgment tag, bit, Report. Класс —
    /// <see cref="AlarmClassName.Format"/> по списку классов MSG_NUMHANDLER. Тега нет —
    /// <c>&lt;No tag&gt;</c>, бит 0, адрес пустой, как в TIA; но если AREAPTR прочитана не целиком
    /// (<see cref="FwxDocument.AlarmBitsComplete"/>), отсутствие тега не известно — имя и бит
    /// <c>?</c>, адрес «неизвестно». У квитирования из ПЛК адреса нет и в TIA. Элемент AREAPTR
    /// есть, но ни один тег VAR на него не ссылается, — имя <c>?</c>, адрес «неизвестно». Адрес —
    /// <see cref="AlarmBitAddress.Format"/>.
    /// </summary>
    /// <returns>10 значений: текст или число.</returns>
    public static ReportCell[] Format(FwxDocument doc, IReadOnlyList<PlcMatch?> matches, AlarmTagIndex tagIndex, Func<long, string> firstLanguageText, DiscreteAlarm alarm)
    {
        var trigger = TagColumns(doc, matches, tagIndex.Find(alarm.Id, AlarmBitRole.Trigger));
        var hmi = TagColumns(doc, matches, tagIndex.Find(alarm.Id, AlarmBitRole.HmiAcknowledgment));
        var plc = TagColumns(doc, matches, tagIndex.Find(alarm.Id, AlarmBitRole.PlcAcknowledgment));
        return
        [
            AlarmClassName.Format(doc.AlarmClasses, alarm.ClassIndex, firstLanguageText),
            trigger.Tag, trigger.Bit, trigger.Address,
            hmi.Tag, hmi.Bit, hmi.Address,
            plc.Tag, plc.Bit,
            alarm.Report ? "On" : "Off"
        ];
    }

    /// <summary>Имя тега, бит и адрес бита для одного назначения.</summary>
    /// <returns>Три значения колонок.</returns>
    private static (ReportCell Tag, ReportCell Bit, ReportCell Address) TagColumns(FwxDocument doc, IReadOnlyList<PlcMatch?> matches, (int? Tag, int Bit)? found)
    {
        if (found is null)
        {
            return doc.AlarmBitsComplete ? (NoTag, 0, "") : ("?", "?", Unknown);
        }
        var (tagIndex, bit) = found.Value;
        if (tagIndex is not { } i || i >= doc.Tags.Count)
        {
            return ("?", bit, Unknown);
        }
        var tag = doc.Tags[i];
        var match = i < matches.Count ? matches[i] : null;
        return (tag.Name, bit, AlarmBitAddress.Format(tag, match, bit));
    }
}
