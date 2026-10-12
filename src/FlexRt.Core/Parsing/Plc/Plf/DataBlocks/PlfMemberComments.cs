using FlexRt.Binary;
using FlexRt.Model.Plc.Plf;

namespace FlexRt.Parsing.Plc.Plf.DataBlocks;

/// <summary>
/// Комментарии членов интерфейса: объект класса 0x0022160d, связанный с владельцем интерфейса
/// (FB, DB или UDT) связью 0x00222618. Раскладка (установлена на файлах пяти проектов,
/// 650 объектов, все разобрались): <c>u32</c> по смещению <c>0x34</c> объекта — указатель
/// <c>p</c> на таблицу (наблюдение: <c>0x48</c>, а у части объектов UDT и FB — смещение дальше
/// по объекту); от <c>p</c>: <c>[u32 ?][u32 n][u16 ?][u32 ?]</c>, <c>n</c> заглушек <c>0x8000001f</c>,
/// <c>n</c> смещений ключей (от <c>p - 4</c>), ключи — цепочки ID членов UTF-16 с нулевым
/// символом (<c>52:51:51</c>), <c>n</c> смещений значений (от конца массива смещений) и
/// значения — мультиязычные тексты подряд (<see cref="PlfText.ReadMultilingual"/>). Ключ с
/// номером i соответствует значению с номером i.
/// </summary>
internal static class PlfMemberComments
{
    /// <summary>Класс объекта комментариев членов.</summary>
    public const long Class = 0x0022160d;

    /// <summary>Тип связи владельца интерфейса с объектом комментариев.</summary>
    public const long Relation = 0x00222618;

    /// <summary>Смещение указателя на таблицу в объекте.</summary>
    private const int TablePointerOffset = 0x34;

    /// <summary>Смещение числа записей от указателя на таблицу.</summary>
    private const int CountOffset = 4;

    /// <summary>Смещение массива заглушек от указателя на таблицу.</summary>
    private const int PlaceholdersOffset = 0x0e;

    /// <summary>Расстояние от указателя на таблицу назад до начала, от которого считаются смещения ключей (наблюдение).</summary>
    private const int KeysBaseShift = 4;

    /// <summary>Размер элемента массива заглушек и смещений.</summary>
    private const int ItemSize = 4;

    /// <summary>Размер нулевого символа UTF-16, которым кончается ключ.</summary>
    private const int TerminatorSize = 2;

    /// <summary>Наибольшее число записей, которое принимается без ошибки (наблюдение: до нескольких сотен).</summary>
    private const long MaxEntries = 0x10000;

    /// <summary>
    /// Прочитать комментарии объекта: ключ — цепочка ID членов, значение — первый непустой
    /// текст. Пустые тексты не попадают в результат; при повторе ключа берётся первый.
    /// Объект без записей даёт пустой словарь.
    /// </summary>
    /// <returns>Непустые комментарии по ключам.</returns>
    /// <exception cref="FwxFormatException">Таблица или значение выходит за объект или не разбирается.</exception>
    public static Dictionary<string, string> Read(FwxBinary b, PlfObject obj)
    {
        var result = new Dictionary<string, string>();
        var end = obj.Offset + obj.Length;
        var pointer = b.D4(obj.Offset + TablePointerOffset);
        var table = obj.Offset + pointer;
        if (pointer < TablePointerOffset || table + PlaceholdersOffset > end)
        {
            throw new FwxFormatException(PlfFormat.Section, obj.Offset + TablePointerOffset, "указатель таблицы комментариев вне объекта");
        }
        var count = b.D4(table + CountOffset);
        if (count == 0)
        {
            return result;
        }
        var offsetsStart = table + PlaceholdersOffset + count * ItemSize;
        if (count > MaxEntries || offsetsStart + count * ItemSize > end)
        {
            throw new FwxFormatException(PlfFormat.Section, table + CountOffset, $"в таблице комментариев {count} записей");
        }
        var (keys, valuesStart) = ReadKeys(b, table, offsetsStart, count, end);
        var valuesBase = valuesStart + count * ItemSize;
        for (var i = 0; i < count; i++)
        {
            var (text, _) = PlfText.ReadMultilingual(b, valuesBase + b.D4(valuesStart + i * ItemSize), end);
            if (text.Length > 0)
            {
                result.TryAdd(keys[i], text);
            }
        }
        return result;
    }

    /// <summary>Ключи по их смещениям и смещение массива смещений значений (сразу за самым дальним ключом).</summary>
    /// <returns>Ключи в порядке записей и смещение массива смещений значений.</returns>
    /// <exception cref="FwxFormatException">Ключ выходит за объект или не кончается нулевым символом.</exception>
    private static (List<string> Keys, long ValuesStart) ReadKeys(FwxBinary b, long table, long offsetsStart, long count, long end)
    {
        var keys = new List<string>();
        var valuesStart = offsetsStart + count * ItemSize;
        for (var i = 0; i < count; i++)
        {
            var start = table - KeysBaseShift + b.D4(offsetsStart + i * ItemSize);
            var length = KeyLength(b, start, end);
            keys.Add(b.GetNameLen(start, length));
            valuesStart = Math.Max(valuesStart, start + (length + 1) * TerminatorSize);
        }
        return (keys, valuesStart);
    }

    /// <summary>Длина ключа в символах UTF-16 до нулевого символа.</summary>
    /// <returns>Число символов без нулевого.</returns>
    /// <exception cref="FwxFormatException">Ключ начинается вне объекта или нулевого символа нет до его конца.</exception>
    private static int KeyLength(FwxBinary b, long start, long end)
    {
        if (start < 0)
        {
            throw new FwxFormatException(PlfFormat.Section, start, "ключ комментария вне объекта");
        }
        var length = 0;
        while (start + (length + 1) * TerminatorSize <= end)
        {
            if (b.D2(start + length * TerminatorSize) == 0)
            {
                return length;
            }
            length++;
        }
        throw new FwxFormatException(PlfFormat.Section, start, "ключ комментария без нулевого символа");
    }
}
