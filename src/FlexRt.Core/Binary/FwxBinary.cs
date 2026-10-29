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

    /// <summary>
    /// 64-битное слово, little endian: два 32-битных слова <see cref="D4"/>, младшее первым.
    /// Границы проверяются у каждого слова отдельно.
    /// </summary>
    /// <exception cref="FwxFormatException">Слово выходит за пределы файла.</exception>
    public long D8(long pos) => D4(pos) | (D4(pos + 4) << 32);

    /// <summary>Число float64 (IEEE 754 double), little endian: биты 64-битного слова <see cref="D8"/>.</summary>
    /// <exception cref="FwxFormatException">Число выходит за пределы файла.</exception>
    public double F64(long pos) => BitConverter.Int64BitsToDouble(D8(pos));

    /// <summary>
    /// 32-битное слово без знака, big endian: первый байт — старший. Так в панели хранятся IP-адрес
    /// и маска (<c>c0 a8 00 01</c> = 192.168.0.1).
    /// </summary>
    /// <exception cref="FwxFormatException">Слово выходит за пределы файла.</exception>
    public uint D4BigEndian(long pos)
    {
        Check(pos, 4);
        return (uint)((data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3]);
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
}
