using System.Globalization;
using System.Xml.Linq;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc;

/// <summary>
/// Элемент <c>&lt;Member&gt;</c> интерфейса блока в модели <see cref="PlcDbMember"/>. Атрибуты
/// одни и те же в XML интерфейса PEData.plf и карты ПЛК.
/// </summary>
internal static class PlcDbMemberXml
{
    /// <summary>Член модели из элемента: отсутствующие атрибуты — пустые строки, <c>RID</c> и <c>LID</c>, которых нет или которые не числа, — -1.</summary>
    /// <returns>Член.</returns>
    public static PlcDbMember Read(XElement member) => new(
        (string?)member.Attribute("ID") ?? "",
        (string?)member.Attribute("Name") ?? "",
        (string?)member.Attribute("Type") ?? "",
        ParseHex((string?)member.Attribute("RID")),
        long.TryParse((string?)member.Attribute("LID"), out var lid) ? lid : -1);

    /// <summary>Число вида <c>0x02000008</c>.</summary>
    /// <returns>Значение или -1, если атрибута нет или он не число.</returns>
    private static long ParseHex(string? text)
    {
        var digits = text is not null && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : null;
        return digits is not null && long.TryParse(digits, NumberStyles.HexNumber, null, out var value) ? value : -1;
    }
}
