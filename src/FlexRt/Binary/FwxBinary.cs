using System.Text;

namespace FlexRt.Binary;

/// <summary>Чтение примитивов из FWX. Все значения little endian.</summary>
/// <param name="data">Содержимое файла целиком.</param>
public sealed class FwxBinary(byte[] data)
{
    /// <summary>Размер файла в байтах.</summary>
    public int Length => data.Length;

    /// <summary>Имя секции, которая сейчас разбирается — попадает в сообщение об ошибке.</summary>
    public string Section { get; set; } = "HEADER";

    /// <summary>Проверить, что диапазон лежит внутри файла.</summary>
    /// <exception cref="FwxFormatException">Диапазон выходит за пределы файла.</exception>
    public void Check(long pos, long length)
    {
        if (pos < 0 || length < 0 || pos + length > data.Length)
        {
            throw new FwxFormatException(Section, pos, $"выход за пределы файла (нужно {length} байт, размер файла {data.Length})");
        }
    }

    /// <summary>Байт без знака.</summary>
    public int D1(long pos)
    {
        Check(pos, 1);
        return data[pos];
    }

    /// <summary>16-битное слово без знака, little endian.</summary>
    public int D2(long pos)
    {
        Check(pos, 2);
        return data[pos] | (data[pos + 1] << 8);
    }

    /// <summary>32-битное слово без знака, little endian.</summary>
    public long D4(long pos)
    {
        Check(pos, 4);
        return (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
    }

    /// <summary>Строка UTF-16LE длиной <paramref name="chars"/> символов.</summary>
    public string GetNameLen(long pos, int chars)
    {
        if (chars <= 0)
        {
            return "";
        }
        Check(pos, chars * 2L);
        return Encoding.Unicode.GetString(data, (int)pos, chars * 2);
    }

    /// <summary>Строка UTF-8 длиной <paramref name="length"/> байт.</summary>
    /// <exception cref="FwxFormatException">Строка выходит за пределы файла.</exception>
    public string GetUtf8(long pos, int length)
    {
        Check(pos, length);
        return Encoding.UTF8.GetString(data, (int)pos, length);
    }

    /// <summary>Байты диапазона без копирования.</summary>
    /// <exception cref="FwxFormatException">Диапазон выходит за пределы файла.</exception>
    public ReadOnlySpan<byte> Span(long pos, long length)
    {
        Check(pos, length);
        return data.AsSpan((int)pos, (int)length);
    }

    /// <summary>Строка UTF-16LE, длина (в символах) лежит в первом 16-битном слове.</summary>
    public string GetName(long pos) => GetNameLen(pos + 2, D2(pos));

    /// <summary>Байты как двузначные hex через пробел: <c>ef be 0c 00</c>.</summary>
    public string ToHex(long pos, long length)
    {
        Check(pos, length);
        var sb = new StringBuilder((int)Math.Min(length * 3, int.MaxValue / 2));
        for (long i = 0; i < length; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }
            sb.Append(data[pos + i].ToString("x2"));
        }
        return sb.ToString();
    }
}
