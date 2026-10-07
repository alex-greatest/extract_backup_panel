using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Codes;

/// <summary>Логический адрес PLC-тега с абсолютным доступом, как в TIA.</summary>
public static class PlcAddress
{
    /// <summary>
    /// Адрес по области, размеру и номеру байта/бита: <c>%I1.5</c> (бит), <c>%IB2</c>,
    /// <c>%IW44</c>, <c>%QD50</c>, DB — <c>%DB80.DBW46</c>, <c>%DB80.DBX0.1</c>; 64 бита,
    /// массивы и строки — адрес начала с битом (<c>%I32.0</c>, <c>%DB81.DBX96.0</c>, как в
    /// таблице тегов ПЛК); таймер и счётчик — <c>%T0</c>, <c>%C0</c>. Вид 64-битного адреса и
    /// массивов в колонке HMI не проверялся.
    /// </summary>
    /// <returns>Адрес или <c>null</c>: символьный доступ или неизвестная область.</returns>
    public static string? Format(PlcLink link)
    {
        if (!link.Absolute || link.Area is null)
        {
            return null;
        }
        if (link.Area is PlcArea.Timer or PlcArea.Counter)
        {
            return $"%{(link.Area == PlcArea.Timer ? 'T' : 'C')}{link.ByteOffset}";
        }

        var prefix = link.Area switch
        {
            PlcArea.Input => "%I",
            PlcArea.Output => "%Q",
            PlcArea.Memory => "%M",
            _ => $"%DB{link.DbNumber}.DB"
        };
        // у DB бит адресуется через X: %DB80.DBX0.1; у I/Q/M — без буквы: %I1.5
        var bitLetter = link.Area == PlcArea.DataBlock ? "X" : "";
        var size = link.Elements > 1 ? 0 : link.BitSize;
        return size switch
        {
            1 => $"{prefix}{bitLetter}{link.ByteOffset}.{link.Bit}",
            8 => $"{prefix}B{link.ByteOffset}",
            16 => $"{prefix}W{link.ByteOffset}",
            32 => $"{prefix}D{link.ByteOffset}",
            _ => $"{prefix}{bitLetter}{link.ByteOffset}.{link.Bit}"
        };
    }
}
