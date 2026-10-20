using System.Buffers.Binary;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Испорченная копия образца панели: <c>pdata.fwc</c> из <c>samples/</c> копируется во временный каталог теста, в элементе
/// таблицы на заданном смещении записывается число little-endian. Элемент находится разбором той же копии
/// (<see cref="FwxReader.Items"/>). Ширина поля задаётся методом (<see cref="U16"/> или <see cref="U32"/>), а не
/// числом, чтобы поле u32 не переписывалось наполовину.
/// </summary>
public static class CorruptedCopy
{
    /// <summary>Имя копии во временном каталоге.</summary>
    private const string FileName = "pdata.fwc";

    /// <summary>Записать u16 в элемент таблицы копии образца.</summary>
    /// <param name="dir">Временный каталог теста.</param>
    /// <param name="sample">Образец относительно корня репозитория.</param>
    /// <param name="table">Таблица TOC.</param>
    /// <param name="index">Номер элемента с нуля.</param>
    /// <param name="offset">Смещение поля от начала элемента.</param>
    /// <param name="value">Новое значение поля.</param>
    /// <returns>Путь копии и испорченный элемент (смещение и длина — для текста ожидаемого предупреждения).</returns>
    public static (string Path, TableItem Item) U16(TestDirectory dir, string sample, string table, int index, int offset, ushort value)
    {
        var bytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        return Write(dir, sample, table, index, offset, bytes);
    }

    /// <summary>Записать u32 в элемент таблицы копии образца.</summary>
    /// <param name="dir">Временный каталог теста.</param>
    /// <param name="sample">Образец относительно корня репозитория.</param>
    /// <param name="table">Таблица TOC.</param>
    /// <param name="index">Номер элемента с нуля.</param>
    /// <param name="offset">Смещение поля от начала элемента.</param>
    /// <param name="value">Новое значение поля.</param>
    /// <returns>Путь копии и испорченный элемент (смещение и длина — для текста ожидаемого предупреждения).</returns>
    public static (string Path, TableItem Item) U32(TestDirectory dir, string sample, string table, int index, int offset, uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return Write(dir, sample, table, index, offset, bytes);
    }

    /// <summary>Скопировать образец, найти элемент таблицы и записать в него байты поля.</summary>
    /// <returns>Путь копии и элемент.</returns>
    private static (string Path, TableItem Item) Write(TestDirectory dir, string sample, string table, int index, int offset, byte[] field)
    {
        var input = dir.CopyFromRepo(sample, FileName);
        var doc = FwxReader.Read(input);
        var item = FwxReader.Items(doc.Binary, doc.FindTable(table)!).ElementAt(index);
        var bytes = File.ReadAllBytes(input);
        field.CopyTo(bytes.AsSpan((int)item.Offset + offset));
        File.WriteAllBytes(input, bytes);
        return (input, item);
    }
}
