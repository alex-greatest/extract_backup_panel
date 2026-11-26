using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Хвост SCREEN с аппаратными клавишами, подтверждённый на шагах TP700 Comfort.</summary>
internal static class ScreenDirectKeyReader
{
    /// <summary>Метка блока сразу после цвета фона.</summary>
    private const int Marker = 0x14ff;

    /// <summary>Размер заголовка до первой клавиши: u32 метка, u32 размер, u16 0xffff и u16 число клавиш.</summary>
    private const int HeaderSize = 0x0c;

    /// <summary>Размер клавиши: четыре координаты i16 и номер бита u16.</summary>
    private const int EntrySize = 0x0a;

    /// <summary>Прочитать рамки клавиш и положение цвета перед подтверждённым блоком.</summary>
    /// <returns>Смещение цвета и клавиши. При повреждении блока — цвет и пустой список с предупреждением.</returns>
    public static (long? ColorOffset, IReadOnlyList<(ScreenRect Bounds, int Bit)> Keys) Read(
        FwxDocument doc, TableItem item, long objectsEnd, int count)
    {
        var binary = doc.Binary;
        // После пар объектов: u16 на объект с выравниванием до 4 байт, два u32 и цвет.
        var marker = objectsEnd + (count + 1L) / 2 * 4 + 0x0c;
        if (marker + 4 > item.Length || binary.D4(item.Offset + marker) != Marker)
        {
            return (null, []);
        }
        var start = item.Offset + marker;
        if (EntryCount(binary, item, marker) is not { } entries)
        {
            doc.Warnings.Add(new FwxFormatException("SCREEN", start,
                $"запись {item.Index + 1}: повреждён блок DirectKey").Message);
            return (marker - 4, []);
        }
        var keys = new List<(ScreenRect Bounds, int Bit)>();
        for (var index = 0; index < entries; index++)
        {
            var entry = start + HeaderSize + index * EntrySize;
            var left = (short)binary.D2(entry);
            var top = (short)binary.D2(entry + 2);
            var right = (short)binary.D2(entry + 4);
            var bottom = (short)binary.D2(entry + 6);
            var bit = binary.D2(entry + 8);
            if (bit <= 31)
            {
                keys.Add((new ScreenRect(left, top, right - left, bottom - top), bit));
            }
        }
        return (marker - 4, keys);
    }

    /// <summary>Проверить границы заголовка и списка клавиш.</summary>
    /// <returns>Число записей. Повреждённый блок — null.</returns>
    private static int? EntryCount(FwxBinary binary, TableItem item, long marker)
    {
        if (marker + HeaderSize > item.Length)
        {
            return null;
        }
        var start = item.Offset + marker;
        var size = binary.D4(start + 4);
        var entries = binary.D2(start + 0x0a);
        return binary.D2(start + 8) == ushort.MaxValue && size == 4 + entries * EntrySize
            && marker + 8 + size <= item.Length ? entries : null;
    }
}
