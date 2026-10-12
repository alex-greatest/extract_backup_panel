using System.Buffers.Binary;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>
/// Начало объекта в файле <c>OMSSTORE</c> карты ПЛК: байт <see cref="Mark"/>, u32 RID big endian,
/// varint и байт <see cref="BodyTag"/>, с которого начинается тело объекта.
/// </summary>
internal static class CardObjectStart
{
    /// <summary>Байт начала объекта: за ним u32 RID big endian (наблюдение: <c>a6 8a 0e 04 42</c> — DB1090).</summary>
    private const byte Mark = 0xa6;

    /// <summary>Начало объекта до varint: <see cref="Mark"/> и u32 RID.</summary>
    public const int Size = 5;

    /// <summary>
    /// Байт, которым начинается тело объекта сразу за varint после RID (наблюдение: <c>00 a3</c>,
    /// <c>20 a3</c>, <c>84 80 80 80 20 a3</c> у всех 587 объектов карты S7-1500, TIA V19; у 74
    /// случайных совпадений <c>a6 89/8a</c> его нет).
    /// </summary>
    private const byte BodyTag = 0xa3;

    /// <summary>Наибольшая длина varint за RID в байтах (u32).</summary>
    private const int MaxVarintLength = 5;

    /// <summary>
    /// Начало ли объекта на позиции: <see cref="Mark"/>, целый RID в файле, за varint после RID —
    /// <see cref="BodyTag"/>. Старший байт RID не проверяется: это делает вызывающий, если нужно.
    /// </summary>
    /// <returns><c>true</c>, если на позиции начало объекта.</returns>
    public static bool IsAt(byte[] data, int pos)
    {
        if (data[pos] != Mark || pos + Size > data.Length)
        {
            return false;
        }
        var end = Math.Min(data.Length, pos + Size + MaxVarintLength);
        var at = pos + Size;
        while (at < end && (data[at] & 0x80) != 0)
        {
            at++;
        }
        return at + 1 < data.Length && data[at + 1] == BodyTag;
    }

    /// <summary>RID объекта, начало которого на позиции (<see cref="IsAt"/>).</summary>
    /// <returns>RID: u32 big endian за <see cref="Mark"/>.</returns>
    public static long Rid(byte[] data, int pos) => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos + 1));
}
