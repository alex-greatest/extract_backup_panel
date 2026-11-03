using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Tags;

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

    /// <summary>
    /// Бит кода типа у тегов ПЛК (наблюдение: A603A0097 — <c>0x82</c> у Int, <c>0x84</c> у Real); значение бита не
    /// проверено, в <see cref="Format"/> такой код — <c>?</c>.
    /// </summary>
    private const int PlcBit = 0x80;

    /// <summary>
    /// Тип с дробной частью — Real или LReal, без бита массива и бита <see cref="PlcBit"/>: у I/O field с таким тегом
    /// знаки после запятой задаёт шаблон, у целого — сдвиг запятой (сверено с TIA на всех полях A603A0097).
    /// </summary>
    /// <returns><c>true</c> — Real или LReal.</returns>
    public static bool IsFloat(int typeCode) =>
        Names.GetValueOrDefault(typeCode & ~HmiTag.ArrayFlag & ~PlcBit) is "Real" or "LReal";
}
