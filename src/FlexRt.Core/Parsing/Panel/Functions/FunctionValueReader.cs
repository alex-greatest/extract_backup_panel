using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>
/// Параметр-VARIANT функции (вид 10): блок 16 байт — u16 тип, u16 слово, 4 байта, 8 байт значения (сверено с TIA на
/// 148 функциях копий <c>data/real/steps-events/</c> 20–22, V21).
/// </summary>
internal static class FunctionValueReader
{
    /// <summary>Размер блока VARIANT.</summary>
    public const int Size = 0x10;

    /// <summary>Тип 3: целое i32 на +8 (слово +2 равно 0) или объект (слово +2 равно <see cref="ObjectFlag"/>).</summary>
    private const int IntegerType = 3;

    /// <summary>Тип 5: f64 на +8 (SetTag, IncreaseTag, LinearScaling).</summary>
    private const int RealType = 5;

    /// <summary>Тип 8: строка, на +8 — u32 смещение текста от начала блока, перед текстом u32 длина в байтах без нуля.</summary>
    private const int TextType = 8;

    /// <summary>Тип 0x12: объект проекта, на +8 u32 номер, на +0x0c u32 вид (0x44 журнал, 0xe0 отчёт, 0x34 текстовый список).</summary>
    private const int ProjectObjectType = 0x12;

    /// <summary>Тип 0x13: целое u32 на +8 (номер записи рецепта, Slot, Rack, Bits to mask).</summary>
    private const int UInt32Type = 0x13;

    /// <summary>Слово +2 типа 3 у целого.</summary>
    private const int IntegerFlag = 0;

    /// <summary>Слово +2 типа 3 у объекта: на +8 u16 номер элемента, на +0x0a u16 Id таблицы, на +0x0c <c>ffffffff</c>.</summary>
    private const int ObjectFlag = 2;

    /// <summary>Смещение слова за типом.</summary>
    private const int FlagOffset = 2;

    /// <summary>Смещение значения в блоке.</summary>
    private const int ValueOffset = 8;

    /// <summary>Смещение u16 Id таблицы объекта (за u16 номером элемента на +8).</summary>
    private const int ObjectTableOffset = 0x0a;

    /// <summary>Смещение u32 вида объекта проекта.</summary>
    private const int ProjectKindOffset = 0x0c;

    /// <summary>Размер u32 длины строки перед текстом.</summary>
    private const int TextLengthSize = 4;

    /// <summary>
    /// Прочитать блок VARIANT. Незнакомый тип (или тип 3 со словом не 0 и не 2) — <see cref="FunctionArgumentKind.Unknown"/>
    /// и предупреждение с таблицей, номером записи и смещением.
    /// </summary>
    /// <param name="doc">Документ панели: в него пишется предупреждение.</param>
    /// <param name="table">Таблица функции — для сообщений.</param>
    /// <param name="item">Элемент функции.</param>
    /// <param name="pos">Смещение блока от начала файла; блок целиком в элементе.</param>
    /// <param name="parameter">Номер параметра с единицы — для предупреждения.</param>
    /// <returns>Параметр.</returns>
    /// <exception cref="FwxFormatException">Строка не помещается в элемент или объект не сходится.</exception>
    public static FunctionArgument Read(FwxDocument doc, TocEntry table, TableItem item, long pos, int parameter)
    {
        var b = doc.Binary;
        var type = b.D2(pos);
        var flag = b.D2(pos + FlagOffset);
        return type switch
        {
            IntegerType when flag == IntegerFlag => new FunctionArgument(FunctionArgumentKind.Integer, Number: (int)b.D4(pos + ValueOffset)),
            IntegerType when flag == ObjectFlag => ScreenObjects.Argument(doc, b.D2(pos + ObjectTableOffset), b.D2(pos + ValueOffset)),
            RealType => new FunctionArgument(FunctionArgumentKind.Real, Real: b.F64(pos + ValueOffset)),
            TextType => new FunctionArgument(FunctionArgumentKind.Text, Text: Text(b, table, item, pos)),
            ProjectObjectType => new FunctionArgument(
                FunctionArgumentKind.ProjectObject,
                Number: b.D4(pos + ValueOffset),
                ObjectKind: b.D4(pos + ProjectKindOffset)),
            UInt32Type => new FunctionArgument(FunctionArgumentKind.UInt32, Number: b.D4(pos + ValueOffset)),
            _ => Unknown(doc, table, item, parameter, type, flag)
        };
    }

    /// <summary>
    /// Строка блока типа 8: текст UTF-16LE на смещении из блока (от начала блока), перед ним u32 длина в байтах без
    /// нулевого символа; строки лежат за всеми блоками (сверено на 19 строках копии 20).
    /// </summary>
    /// <returns>Текст как есть (у StartProgram TIA сам берёт имя программы в кавычки).</returns>
    /// <exception cref="FwxFormatException">Длина или текст не помещаются в элемент, длина нечётная.</exception>
    private static string Text(FwxBinary b, TocEntry table, TableItem item, long pos)
    {
        var shift = b.D4(pos + ValueOffset);
        var start = pos + shift;
        var end = item.Offset + item.Length;
        if (shift < Size + TextLengthSize || start > end)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: смещение строки 0x{shift:x} вне элемента");
        }
        var length = b.D4(start - TextLengthSize);
        if (length % 2 != 0 || start + length > end)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: строка {length} байт не помещается в элемент");
        }
        return b.GetNameLen(start, (int)(length / 2));
    }

    /// <summary>Незнакомый тип VARIANT: предупреждение и значение «не известно».</summary>
    /// <returns>Параметр <see cref="FunctionArgumentKind.Unknown"/>.</returns>
    private static FunctionArgument Unknown(FwxDocument doc, TocEntry table, TableItem item, int parameter, int type, int flag)
    {
        doc.Warnings.Add(
            $"{table.Name}: запись {item.Index + 1} @ 0x{item.Offset:X}: тип параметра {parameter} 0x{type:x} (слово 0x{flag:x}) не известен, параметр не прочитан");
        return new FunctionArgument(FunctionArgumentKind.Unknown);
    }
}
