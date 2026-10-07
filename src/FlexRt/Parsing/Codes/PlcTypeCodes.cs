using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Codes;

/// <summary>Код типа ПЛК (DATALINK_READWR <c>+0x1d</c>) → названия типов, как в TIA.</summary>
public static class PlcTypeCodes
{
    /// <summary>
    /// Коды, подтверждённые тегами известного типа (TIA V21 / S7-1500). Пары Char/USInt и
    /// DInt/Time дают один код и по файлу панели не различаются; S5Time и Timer различаются
    /// только областью.
    /// </summary>
    private static readonly Dictionary<int, string[]> Names = new()
    {
        [0x00] = ["SInt"],
        [0x01] = ["Byte"],
        [0x02] = ["Int"],
        [0x03] = ["Word"],
        [0x04] = ["DInt", "Time"],
        [0x05] = ["DWord"],
        [0x06] = ["Real"],
        [0x07] = ["Bool"],
        [0x08] = ["String"],
        [0x09] = ["S5Time"],
        [0x0a] = ["Counter"],
        [0x0b] = ["Date"],
        [0x0c] = ["Time_Of_Day"],
        // наблюдение по одному элементу DATALINK (дата и время ПЛК), не по тегу
        [0x0d] = ["Date_And_Time"],
        [0x0f] = ["LInt"],
        [0x10] = ["ULInt"],
        [0x11] = ["USInt", "Char"],
        [0x12] = ["UInt"],
        [0x13] = ["UDInt"],
        [0x15] = ["LReal"],
        [0x16] = ["LTime"],
        [0x17] = ["LDT"],
        [0x18] = ["LTime_Of_Day"],
        [0x21] = ["WChar"]
    };

    /// <summary>Тип известен таблице кодов: только его и можно сверять с кодом из панели.</summary>
    /// <returns><c>true</c>, если такое название типа есть среди кодов (или это Timer).</returns>
    public static bool IsKnown(string typeName) => typeName == "Timer" || Names.Values.Any(n => n.Contains(typeName));

    /// <summary>Код S5Time и Timer: в области T это всегда Timer.</summary>
    private const int S5TimeOrTimer = 0x09;

    /// <summary>
    /// Возможные типы ПЛК для связи: обычно один, у неразличимых пар два. Код 0x09 в
    /// области T — Timer, в других — S5Time.
    /// </summary>
    /// <returns>Названия типов; пусто, если код не встречался.</returns>
    public static IReadOnlyList<string> Candidates(PlcLink link)
    {
        if (link is { PlcTypeCode: S5TimeOrTimer, Area: PlcArea.Timer })
        {
            return ["Timer"];
        }
        return Names.TryGetValue(link.PlcTypeCode, out var names) ? names : [];
    }
}
