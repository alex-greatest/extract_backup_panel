using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>Вызовы FUNC_SCRIPT из шаговых компиляций с VB Sub и Function, включая возвращаемое значение.</summary>
internal static class ScriptFunctionReader
{
    /// <summary>Размер фиксированного заголовка до аргументов.</summary>
    private const int HeaderSize = 0x14;

    /// <summary>Число аргументов: u16 в последних двух байтах заголовка FUNC_SCRIPT.</summary>
    private const int ArgumentCountOffset = 0x12;

    /// <summary>Прочитать номер сценария, назначение результата и параметры вызова VB. Имён сценариев в проверенном FWC нет.</summary>
    /// <returns>Вызов VB с номером сценария из записи.</returns>
    /// <exception cref="FwxFormatException">Повреждён заголовок или параметр не помещается в элемент.</exception>
    public static FunctionCall Read(FwxDocument doc, TocEntry table, TableItem item)
    {
        Check(table, item, 0, HeaderSize);
        var b = doc.Binary;
        var p = item.Offset;
        if (b.D4(p) != 1 || b.D4(p + 4) != 0)
        {
            throw new FwxFormatException(table.Name, p, "неизвестный заголовок вызова VB");
        }
        var script = b.D2(p + 0x0a);
        var result = new VarReference(b.D2(p + 0x0c), b.D2(p + 0x0e));
        var count = b.D2(p + ArgumentCountOffset);
        var arguments = new List<FunctionArgument>();
        var names = new List<string>();
        var cursor = (long)HeaderSize;
        if (result.Table != 0)
        {
            arguments.Add(FunctionReferences.Argument(doc, result));
            names.Add("Результат");
        }
        for (var index = 0; index < count; index++)
        {
            arguments.Add(Argument(doc, table, item, ref cursor));
            names.Add($"Параметр {index + 1}");
        }
        return new FunctionCall(new VarReference(table.Id, item.Index), table.Name, script,
            [$"VB-скрипт #{script}"], names, arguments);
    }

    /// <summary>Прочитать заголовок параметра и проверенную раскладку значения.</summary>
    /// <returns>Тег, целое или строка UTF-16LE.</returns>
    /// <exception cref="FwxFormatException">Неизвестный вид параметра или значение не помещается.</exception>
    private static FunctionArgument Argument(FwxDocument doc, TocEntry table, TableItem item, ref long cursor)
    {
        Check(table, item, cursor, 4);
        var b = doc.Binary;
        var header = item.Offset + cursor;
        var flags = b.D2(header);
        var type = b.D2(header + 2);
        cursor += 4;
        if (flags == 1 && type == 1)
        {
            Check(table, item, cursor, 4);
            var tags = doc.FindTable("VAR");
            var tag = b.D2(item.Offset + cursor);
            cursor += 4;
            return tags is null ? new FunctionArgument(FunctionArgumentKind.Unknown)
                : new FunctionArgument(FunctionArgumentKind.Tag, Reference: new VarReference(tags.Id, tag));
        }
        if (flags == 0 && type == 3)
        {
            Check(table, item, cursor, 4);
            var value = (int)b.D4(item.Offset + cursor);
            cursor += 4;
            return new FunctionArgument(FunctionArgumentKind.Integer, Number: value);
        }
        if (flags == 0 && type == 8)
        {
            Check(table, item, cursor, 4);
            var bytes = b.D4(item.Offset + cursor);
            cursor += 4;
            Check(table, item, cursor, bytes);
            if (bytes >= 2 && bytes % 2 == 0 && b.D2(item.Offset + cursor + bytes - 2) == 0)
            {
                var value = b.GetNameLen(item.Offset + cursor, (int)(bytes / 2 - 1));
                cursor += bytes;
                return new FunctionArgument(FunctionArgumentKind.Text, Text: value);
            }
        }
        throw new FwxFormatException(table.Name, header, $"тип параметра вызова VB 0x{type:x}, флаги 0x{flags:x}");
    }

    /// <summary>Проверить границу элемента вызова, включая переменную длину строки.</summary>
    /// <exception cref="FwxFormatException">Диапазон выходит за элемент.</exception>
    private static void Check(TocEntry table, TableItem item, long offset, long size)
    {
        if (offset + size > item.Length)
        {
            throw new FwxFormatException(table.Name, item.Offset + offset, $"запись {item.Index + 1}: вызов VB выходит за элемент");
        }
    }
}
