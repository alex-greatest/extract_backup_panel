using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Codes;

/// <summary>Тип члена DB по атрибутам <c>Type</c> и <c>RID</c>.</summary>
public static class PlcMemberTypes
{
    /// <summary>Старшие 24 бита <c>RID</c> скаляра простого типа; младший байт — код типа.</summary>
    private const long ScalarBase = 0x02000000;

    /// <summary>
    /// Коды скаляров в <c>RID</c> (<c>0x020000TT</c>). Установлено на тегах DB проекта A603A0097
    /// по совпадению с кодом типа в DATALINK_READWR: 01, 02, 04, 05, 08, 34, 35 и 13 (String).
    /// 07 (DInt) — со слов пользователя, на файле не проверено.
    /// </summary>
    private static readonly Dictionary<long, string> Scalars = new()
    {
        [0x01] = "Bool",
        [0x02] = "Byte",
        [0x04] = "Word",
        [0x05] = "Int",
        [0x07] = "DInt",
        [0x08] = "Real",
        [0x13] = "String",
        [0x34] = "USInt",
        [0x35] = "UInt"
    };

    /// <summary>
    /// Тип члена: атрибут <c>Type</c>, если он есть, иначе имя скаляра по <c>RID</c>.
    /// Неизвестный код — пустая строка, без догадок.
    /// </summary>
    /// <returns>Тип как в TIA или пустая строка.</returns>
    public static string Of(PlcDbMember member)
    {
        if (member.Type.Length > 0)
        {
            return member.Type;
        }
        var isScalar = member.Rid >= 0 && (member.Rid & ~0xffL) == ScalarBase;
        return isScalar && Scalars.TryGetValue(member.Rid & 0xff, out var name) ? name : "";
    }
}
