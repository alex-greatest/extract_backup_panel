using System.Globalization;

namespace FlexRt.Export.PanelData;

/// <summary>Значение предела-константы аналогового аларма для колонки Limit.</summary>
public static class AnalogLimitValue
{
    /// <summary>Байт вида значения: целое со знаком (наблюдение: тег срабатывания Int).</summary>
    private const int SignedKind = 1;

    /// <summary>Байт вида значения: float32 (наблюдение: тег срабатывания Real).</summary>
    private const int FloatKind = 2;

    /// <summary>Байт вида значения: целое без знака (наблюдение: тег срабатывания UInt; у предела-тега тоже 0).</summary>
    private const int UnsignedKind = 0;

    /// <summary>
    /// Значение по байту вида (гипотеза по четырём алармам A603A0097): целое со знаком и без знака
    /// — числом, float32 — текстом в инвариантной культуре (<c>12.5</c>, <c>13</c>, как в TIA),
    /// незнакомый вид — <c>?</c>.
    /// </summary>
    /// <param name="kind">Байт вида значения.</param>
    /// <param name="value">4 байта значения как u32.</param>
    /// <returns>Значение ячейки Limit.</returns>
    public static ReportCell Format(int kind, long value) => kind switch
    {
        SignedKind => unchecked((int)value),
        UnsignedKind => value,
        FloatKind => BitConverter.Int32BitsToSingle(unchecked((int)value)).ToString(CultureInfo.InvariantCulture),
        _ => "?"
    };
}
