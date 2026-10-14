using System.Globalization;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Значение предела-константы аналогового аларма для колонки Limit.</summary>
public static class AnalogLimitValue
{
    /// <summary>Байт вида значения: целое со знаком (наблюдение: тег срабатывания Int).</summary>
    private const int SignedKind = 1;

    /// <summary>Байт вида значения: float32 (наблюдение: тег срабатывания Real).</summary>
    private const int FloatKind = 2;

    /// <summary>Байт вида значения: float64 (установлено на Project1: тег LReal, <c>1.5</c>).</summary>
    private const int Float64Kind = 3;

    /// <summary>Байт вида значения: целое без знака (наблюдение: тег срабатывания UInt; у предела-тега тоже 0).</summary>
    private const int UnsignedKind = 0;

    /// <summary>
    /// Значение по байту вида: целое со знаком (сверено: <c>-7</c>, <c>1000</c>) и без знака
    /// (гипотеза) — числом, float32 и float64 — текстом в инвариантной культуре (<c>12.5</c>,
    /// <c>13</c>, <c>1.5</c>, как в TIA), незнакомый вид — <c>?</c>.
    /// </summary>
    /// <param name="kind">Байт вида значения.</param>
    /// <param name="value">Значение: 4 байта как u32, у float64 — 8 байт как u64.</param>
    /// <returns>Значение ячейки Limit.</returns>
    public static ReportCell Format(int kind, long value) => kind switch
    {
        SignedKind => unchecked((int)value),
        UnsignedKind => value,
        FloatKind => BitConverter.Int32BitsToSingle(unchecked((int)value)).ToString(CultureInfo.InvariantCulture),
        Float64Kind => BitConverter.Int64BitsToDouble(value).ToString(CultureInfo.InvariantCulture),
        _ => "?"
    };
}
