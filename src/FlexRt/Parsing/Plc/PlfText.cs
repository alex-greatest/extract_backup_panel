using FlexRt.Binary;

namespace FlexRt.Parsing.Plc;

/// <summary>Числа переменной длины, строки и мультиязычные тексты в объектах PEData.plf.</summary>
internal static class PlfText
{
    /// <summary>Наибольшее число байт varint, которое принимается (u32).</summary>
    private const int MaxVarintBytes = 5;

    /// <summary>Наибольшее число языков в тексте, которое принимается без ошибки (наблюдение: 0..5).</summary>
    private const long MaxLanguages = 0x40;

    /// <summary>Длина хвоста мультиязычного текста после текстов.</summary>
    private const int MultilingualTail = 0x10;

    /// <summary>
    /// Выполнить чтение, которое может не сойтись с раскладкой: поиск «по шаблону» перебирает
    /// позиции, и несовпадение на одной из них — не ошибка, а повод идти дальше.
    /// </summary>
    /// <returns>Результат чтения или <c>default</c> (<c>null</c>), если бросилась <see cref="FwxFormatException"/>.</returns>
    public static T? Attempt<T>(Func<T> read)
    {
        try
        {
            return read();
        }
        catch (FwxFormatException)
        {
            return default;
        }
    }

    /// <summary>
    /// Число переменной длины (LEB128, как .NET 7-bit): 7 бит на байт, старший бит — есть ли
    /// следующий байт. Установлено на файлах: <c>e5 03</c> = 485.
    /// </summary>
    /// <returns>Значение и смещение сразу за числом.</returns>
    /// <exception cref="FwxFormatException">Число длиннее 5 байт.</exception>
    public static (long Value, long Next) ReadVarint(FwxBinary b, long pos)
    {
        long value = 0;
        for (var i = 0; i < MaxVarintBytes; i++)
        {
            var part = b.D1(pos + i);
            value |= (long)(part & 0x7f) << (7 * i);
            if ((part & 0x80) == 0)
            {
                return (value, pos + i + 1);
            }
        }
        throw new FwxFormatException(PlfFormat.Section, pos, "число переменной длины длиннее 5 байт");
    }

    /// <summary>
    /// Строка: varint «длина + 1», затем UTF-8. Префикс 0 (отсутствующая строка) и 1 (пустая)
    /// дают пустую строку. Строка должна лежать до <paramref name="end"/>.
    /// </summary>
    /// <returns>Текст и смещение сразу за строкой.</returns>
    /// <exception cref="FwxFormatException">Строка выходит за <paramref name="end"/>.</exception>
    public static (string Text, long Next) ReadString(FwxBinary b, long pos, long end)
    {
        var (prefix, start) = ReadVarint(b, pos);
        var length = Math.Max(prefix - 1, 0);
        return start + length <= end
            ? (b.GetUtf8(start, (int)length), start + length)
            : throw new FwxFormatException(PlfFormat.Section, pos, "строка выходит за пределы объекта");
    }

    /// <summary>
    /// Мультиязычный текст (комментарий):
    /// <c>[varint X][u32 A][u32 A-16][ff ff ff ff][u32 n][n × u16 LCID][n × u32 смещение]</c>,
    /// затем тексты <c>[u32 длина][UTF-8]</c> и 16 байт хвоста. X — длина всего блока от его
    /// начала, A = X − длина varint; смещения текстов — от поля A. Установлено на 77 тыс.
    /// блоков пяти проектов.
    /// </summary>
    /// <returns>Первый непустой текст (пустая строка, если таких нет) и смещение за блоком.</returns>
    /// <exception cref="FwxFormatException">Раскладка не сходится или блок выходит за <paramref name="end"/>.</exception>
    public static (string Text, long Next) ReadMultilingual(FwxBinary b, long pos, long end)
    {
        var (total, a) = ReadVarint(b, pos);
        var blockEnd = pos + total;
        var size = b.D4(a);
        if (size != blockEnd - a || b.D4(a + 4) != size - 0x10 || b.D4(a + 8) != 0xFFFFFFFF || blockEnd > end)
        {
            throw new FwxFormatException(PlfFormat.Section, pos, "неизвестная раскладка мультиязычного текста");
        }
        var count = b.D4(a + 0x0c);
        if (count > MaxLanguages)
        {
            throw new FwxFormatException(PlfFormat.Section, a + 0x0c, $"в тексте {count} языков");
        }

        var offsets = a + 0x10 + count * 2;
        for (var i = 0L; i < count; i++)
        {
            var text = a + b.D4(offsets + i * 4);
            var length = b.D4(text);
            if (text + 4 + length > blockEnd - MultilingualTail)
            {
                throw new FwxFormatException(PlfFormat.Section, text, "текст языка выходит за пределы блока");
            }
            if (length > 0)
            {
                return (b.GetUtf8(text + 4, (int)length), blockEnd);
            }
        }
        return ("", blockEnd);
    }
}
