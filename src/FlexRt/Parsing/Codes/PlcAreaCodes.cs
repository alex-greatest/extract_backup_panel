using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Codes;

/// <summary>Коды области памяти ПЛК в pdata.fwc и PEData.plf.</summary>
public static class PlcAreaCodes
{
    /// <summary>
    /// Коды символьного доступа: DATALINK_READWR <c>+0x20</c> и блок доступа тега в PEData.plf.
    /// Установлено на файлах TIA V21 / S7-1500 по тегам с известной областью.
    /// </summary>
    private static readonly Dictionary<long, PlcArea> Symbolic = new()
    {
        [0x50] = PlcArea.Input,
        [0x51] = PlcArea.Output,
        [0x52] = PlcArea.Memory,
        [0x53] = PlcArea.Counter,
        [0x54] = PlcArea.Timer
    };

    /// <summary>
    /// Коды абсолютного доступа: DATALINK_READWR <c>+0x1e</c>. Связаны с символьными
    /// таблицей, а не формулой. Установлено на файлах TIA V21 / S7-1500.
    /// </summary>
    private static readonly Dictionary<long, PlcArea> Absolute = new()
    {
        [0x00] = PlcArea.DataBlock,
        [0x02] = PlcArea.Memory,
        [0x04] = PlcArea.Input,
        [0x05] = PlcArea.Output,
        [0x06] = PlcArea.Counter,
        [0x07] = PlcArea.Timer
    };

    /// <summary>
    /// Старшие 16 бит кода символьного доступа к DB; младшие — номер DB
    /// (<c>0x8a0e0038</c> — DB56). Установлено на 264 тегах DB проекта A603A0097.
    /// </summary>
    private const long DataBlockBase = 0x8a0e0000;

    /// <summary>Область по коду символьного доступа.</summary>
    /// <returns>Область или <c>null</c>, если код не встречался.</returns>
    public static PlcArea? FromSymbolic(long code)
    {
        if (IsDataBlock(code))
        {
            return PlcArea.DataBlock;
        }
        return Symbolic.TryGetValue(code, out var area) ? area : null;
    }

    /// <summary>Номер DB по коду символьного доступа к DB.</summary>
    /// <returns>Номер DB или 0, если код не DB.</returns>
    public static int DbFromSymbolic(long code) => IsDataBlock(code) ? (int)(code & 0xffff) : 0;

    /// <summary>Область по коду абсолютного доступа.</summary>
    /// <returns>Область или <c>null</c>, если код не встречался.</returns>
    public static PlcArea? FromAbsolute(long code) => Absolute.TryGetValue(code, out var area) ? area : null;

    /// <summary>Код символьного доступа принадлежит DB: старшие 16 бит равны <see cref="DataBlockBase"/>.</summary>
    private static bool IsDataBlock(long code) => (code & 0xffff0000) == DataBlockBase;
}
