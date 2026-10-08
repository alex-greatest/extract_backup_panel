using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Название типа тега для вывода — как в колонке Data type в TIA Portal.</summary>
public static class TagTypeNames
{
    /// <summary>
    /// Коды типов без бита массива. Каждый код подтверждён тегом с известным типом в проекте
    /// TIA V17 / TP700 Comfort.
    /// </summary>
    private static readonly Dictionary<int, string> Names = new()
    {
        [0x02] = "Int",
        [0x03] = "DInt",
        [0x04] = "Real",
        [0x05] = "LReal",
        [0x07] = "DateTime",
        [0x08] = "WString",
        [0x0b] = "Bool",
        [0x10] = "SInt",
        [0x11] = "USInt",
        [0x12] = "UInt",
        [0x13] = "UDInt"
    };

    /// <summary>
    /// Название типа: <c>Int</c> у скаляра, <c>Array [0..N-1] of Int</c> у массива.
    /// Нижняя граница всегда 0: TIA не даёт задать другую у HMI-тега.
    /// Неизвестный код (в том числе с другими старшими битами) — <c>?</c>.
    /// </summary>
    /// <returns>Название типа или <c>?</c>.</returns>
    public static string Format(HmiTag tag)
    {
        var isArray = (tag.TypeCode & HmiTag.ArrayFlag) != 0;
        if (!Names.TryGetValue(tag.TypeCode & ~HmiTag.ArrayFlag, out var name))
        {
            return "?";
        }
        return isArray ? $"Array [0..{tag.Elements - 1}] of {name}" : name;
    }
}
