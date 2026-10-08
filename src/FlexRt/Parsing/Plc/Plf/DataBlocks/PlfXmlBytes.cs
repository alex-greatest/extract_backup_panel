using System.IO.Compression;
using System.Text;

namespace FlexRt.Parsing.Plc.Plf.DataBlocks;

/// <summary>
/// Поиск байтов XML интерфейса в объекте PEData.plf: открытый XML или цепочка сжатых zlib-чанков.
/// </summary>
internal static class PlfXmlBytes
{
    /// <summary>Наибольшая длина varint длины сжатого чанка в байтах (наблюдение: 1..3).</summary>
    private const int MaxChunkVarintBytes = 3;

    /// <summary>Наибольший размер распакованного чанка (наблюдение: 4096), с запасом.</summary>
    private const int MaxChunkSize = 0x10000;

    /// <summary>Признак UTF-8 BOM перед XML.</summary>
    private static readonly byte[] Bom = [0xef, 0xbb, 0xbf];

    /// <summary>Начало XML после BOM: корень.</summary>
    private static readonly byte[] RootStart = [.. "<Root"u8];

    /// <summary>Начало XML после BOM: вложенный член.</summary>
    private static readonly byte[] MemberStart = [.. "<Member"u8];

    /// <summary>Байты XML после BOM: открытые или из цепочки сжатых чанков.</summary>
    /// <returns>Байты от <c>&lt;</c> до конца данных или <c>null</c>, если XML не найден.</returns>
    public static byte[]? Find(ReadOnlySpan<byte> data)
    {
        var plain = IndexOfXml(data);
        if (plain >= 0)
        {
            return [.. data[(plain + Bom.Length)..]];
        }
        for (var o = 1; o + 2 <= data.Length; o++)
        {
            // заголовок zlib: 0x78 — метод deflate с окном 32 КБ, второй байт — уровень сжатия
            if (data[o] != 0x78 || data[o + 1] is not (0x01 or 0x5e or 0x9c or 0xda))
            {
                continue;
            }
            for (var length = 1; length <= MaxChunkVarintBytes && length <= o; length++)
            {
                var found = TryChain(data, o - length, length);
                if (found is not null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    /// <summary>Смещение BOM, за которым идёт <c>&lt;Root</c> или <c>&lt;Member</c>.</summary>
    /// <returns>Смещение или <c>-1</c>.</returns>
    private static int IndexOfXml(ReadOnlySpan<byte> data)
    {
        var from = 0;
        while (from < data.Length)
        {
            var i = data[from..].IndexOf(Bom);
            if (i < 0)
            {
                return -1;
            }
            var after = data[(from + i + Bom.Length)..];
            if (after.StartsWith(RootStart) || after.StartsWith(MemberStart))
            {
                return from + i;
            }
            from += i + 1;
        }
        return -1;
    }

    /// <summary>
    /// Цепочка чанков с позиции <paramref name="start"/>, где лежит varint длины длиной
    /// <paramref name="varintLength"/> байт; у следующих чанков длина varint подбирается.
    /// Из склеенных данных берётся XML после BOM.
    /// </summary>
    /// <returns>Байты XML или <c>null</c>, если здесь цепочки нет.</returns>
    private static byte[]? TryChain(ReadOnlySpan<byte> data, int start, int varintLength)
    {
        var all = new List<byte>();
        var pos = start;
        int[] lengths = [varintLength];
        while (ReadChunk(data, pos, lengths) is { } chunk)
        {
            all.AddRange(chunk.Data);
            pos = chunk.Next;
            lengths = [1, 2, MaxChunkVarintBytes];
        }
        var joined = all.ToArray();
        var i = IndexOfXml(joined);
        return i < 0 ? null : [.. joined.AsSpan(i + Bom.Length)];
    }

    /// <summary>Чанк <c>[varint длина][zlib-поток этой длины]</c>; длина varint — одна из <paramref name="lengths"/>.</summary>
    /// <returns>Распакованные данные и позиция следующего чанка или <c>null</c>.</returns>
    private static (byte[] Data, int Next)? ReadChunk(ReadOnlySpan<byte> data, int pos, int[] lengths)
    {
        foreach (var length in lengths)
        {
            var compressed = ReadVarintExact(data, pos, length);
            if (compressed is null || pos + length + compressed.Value > data.Length || data[pos + length] != 0x78)
            {
                continue;
            }
            var inflated = Inflate(data.Slice(pos + length, compressed.Value));
            if (inflated is not null)
            {
                return (inflated, pos + length + compressed.Value);
            }
        }
        return null;
    }

    /// <summary>Varint ровно из <paramref name="length"/> байт: у последнего нет бита продолжения, у остальных он есть.</summary>
    /// <returns>Значение или <c>null</c>, если байты не образуют такой varint.</returns>
    private static int? ReadVarintExact(ReadOnlySpan<byte> data, int pos, int length)
    {
        if (pos < 0 || pos + length >= data.Length)
        {
            return null;
        }
        var value = 0;
        for (var i = 0; i < length; i++)
        {
            var part = data[pos + i];
            value |= (part & 0x7f) << (7 * i);
            var continues = (part & 0x80) != 0;
            var isLast = i == length - 1;
            if (continues == isLast)
            {
                return null;
            }
        }
        return value;
    }

    /// <summary>Распаковать законченный zlib-поток.</summary>
    /// <returns>Данные или <c>null</c>, если поток повреждён или больше допустимого.</returns>
    private static byte[]? Inflate(ReadOnlySpan<byte> compressed)
    {
        try
        {
            using var input = new MemoryStream([.. compressed]);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[MaxChunkSize + 1];
            int read;
            while ((read = zlib.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
                if (output.Length > MaxChunkSize)
                {
                    return null;
                }
            }
            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>Текст XML от начала до закрывающего тега корня; хвост (нули, чужие данные) отбрасывается.</summary>
    /// <returns>Текст или <c>null</c>, если закрывающего тега нет.</returns>
    public static string? CutToClosingTag(byte[] xml)
    {
        var closing = Encoding.ASCII.GetBytes(xml.AsSpan().StartsWith(RootStart) ? "</Root>" : "</Member>");
        var end = xml.AsSpan().IndexOf(closing);
        return end < 0 ? null : Encoding.UTF8.GetString(xml, 0, end + closing.Length);
    }
}
