using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>Вызовы FUNC_SCRIPT из шаговых компиляций с VB Sub и Function, включая возвращаемое значение.</summary>
internal static class ScriptFunctionReader
{
    /// <summary>Размер заголовка до аргументов без списка зависимостей VAR.</summary>
    private const int HeaderSize = 0x14;

    /// <summary>Смещение u16 числа зависимостей VAR.</summary>
    private const int DependencyCountOffset = 0x08;

    /// <summary>Смещение первого индекса зависимости или номера сценария, если зависимостей нет.</summary>
    private const int DependencyStart = 0x0a;

    /// <summary>Размер u16 индекса записи VAR в списке зависимостей.</summary>
    private const int DependencySize = 2;

    /// <summary>Смещение пары результата без списка зависимостей.</summary>
    private const int ResultOffset = 0x0c;

    /// <summary>Смещение служебного u16 относительно пары результата; в проверенных вызовах с зависимостями равно нулю.</summary>
    private const int ResultServiceWordOffset = 0x04;

    /// <summary>Смещение u16 числа аргументов без списка зависимостей.</summary>
    private const int ArgumentCountOffset = 0x12;

    /// <summary>
    /// Прочитать номер сценария, назначение результата и параметры вызова VB. Список зависимостей VAR перед
    /// номером сценария не входит в аргументы. Сдвиг полей результата и параметров подтверждён компиляциями с зависимостями.
    /// </summary>
    /// <returns>Вызов с именем из каталога или подписью «VB-скрипт #N», если имя недоступно.</returns>
    /// <exception cref="FwxFormatException">Заголовок, список зависимостей или аргумент не соответствует проверенной раскладке.</exception>
    public static FunctionCall Read(FwxDocument doc, TocEntry table, TableItem item)
    {
        Check(table, item, 0, HeaderSize);
        var b = doc.Binary;
        var p = item.Offset;
        if (b.D4(p) != 1 || b.D4(p + 4) != 0)
        {
            throw new FwxFormatException(table.Name, p, "неизвестный заголовок вызова VB");
        }
        var dependencyBytes = b.D2(p + DependencyCountOffset) * DependencySize;
        var hasDependencies = dependencyBytes > 0;
        if (hasDependencies)
        {
            CheckDependencies(doc, table, item, dependencyBytes);
        }
        var script = b.D2(p + DependencyStart + dependencyBytes);
        var resultOffset = p + ResultOffset + dependencyBytes;
        var result = ReadResult(doc, table, item, resultOffset, hasDependencies);
        var count = b.D2(p + ArgumentCountOffset + dependencyBytes);
        var arguments = new List<FunctionArgument>();
        var parameterNames = new List<string>();
        var cursor = (long)HeaderSize + dependencyBytes;
        if (result is not null)
        {
            arguments.Add(result);
            parameterNames.Add("Результат");
        }
        for (var index = 0; index < count; index++)
        {
            arguments.Add(Argument(doc, table, item, ref cursor));
            parameterNames.Add($"Параметр {index + 1}");
        }
        var padding = item.Length - cursor;
        if (hasDependencies && (padding is not (0 or 2) || (padding == 2 && b.D2(p + cursor) != 0)))
        {
            throw UnrecognizedDependentTail(table, item, resultOffset);
        }
        var name = script < doc.ScriptNames.Count && doc.ScriptNames[script] is { Length: > 0 } catalogName
            ? catalogName
            : $"VB-скрипт #{script}";
        return new FunctionCall(new VarReference(table.Id, item.Index), table.Name, script, [name], parameterNames, arguments);
    }

    /// <summary>Проверить, что список зависимостей и сдвинутый заголовок помещаются в запись, а индексы VAR существуют.</summary>
    /// <exception cref="FwxFormatException">Заголовок выходит за запись или индекс VAR не существует.</exception>
    private static void CheckDependencies(FwxDocument doc, TocEntry table, TableItem item, int dependencyBytes)
    {
        Check(table, item, DependencyStart, HeaderSize - DependencyStart + dependencyBytes);
        var b = doc.Binary;
        var tagCount = doc.FindTable("VAR")?.Entries ?? 0;
        var dependencies = item.Offset + DependencyStart;
        for (var offset = dependencies; offset < dependencies + dependencyBytes; offset += DependencySize)
        {
            var tag = b.D2(offset);
            if (tag >= tagCount)
            {
                throw new FwxFormatException(table.Name, offset,
                    $"запись {item.Index + 1}: зависимость вызова VB ссылается на отсутствующий элемент VAR {tag + 1}");
            }
        }
    }

    /// <summary>Прочитать привязку результата. У вызова с зависимостями подтверждена цепочка ADAPTLOOPBREAKE → VAR.</summary>
    /// <returns>Значение «Результат» или <c>null</c>, если привязка не задана.</returns>
    /// <exception cref="FwxFormatException">Неразобранная форма результата или повреждённая запись посредника.</exception>
    private static FunctionArgument? ReadResult(FwxDocument doc, TocEntry table, TableItem item, long offset, bool hasDependencies)
    {
        var b = doc.Binary;
        var link = ReferenceHeader.ReadPair(b, offset);
        if (hasDependencies && (b.D2(offset + ResultServiceWordOffset) != 0
            || (link.Table == 0 ? link.Index != 0 : doc.FindTable(link.Table)?.Name != "ADAPTLOOPBREAKE")))
        {
            throw UnrecognizedDependentTail(table, item, offset);
        }
        if (link.Table == 0)
        {
            return null;
        }
        var result = FunctionReferences.Argument(doc, link);
        if (hasDependencies && (result.Reference is not { } tag || tag.Index >= (doc.FindTable("VAR")?.Entries ?? 0)))
        {
            throw UnrecognizedDependentTail(table, item, offset);
        }
        return result;
    }

    /// <summary>Создать ошибку для неподтверждённой раскладки хвоста вызова с зависимостями.</summary>
    /// <returns>Ошибка с позицией пары результата.</returns>
    private static FwxFormatException UnrecognizedDependentTail(TocEntry table, TableItem item, long offset) =>
        new(table.Name, offset, $"запись {item.Index + 1}: хвост вызова VB с зависимостями не разобран");

    /// <summary>Прочитать заголовок параметра и проверенную раскладку значения.</summary>
    /// <returns>Тег, целое, число f64 или строка UTF-16LE.</returns>
    /// <exception cref="FwxFormatException">Неизвестный вид параметра или значение не помещается.</exception>
    private static FunctionArgument Argument(FwxDocument doc, TocEntry table, TableItem item, ref long cursor)
    {
        Check(table, item, cursor, 4);
        var b = doc.Binary;
        var header = item.Offset + cursor;
        var flags = b.D2(header);
        var type = b.D2(header + 2);
        cursor += 4;
        switch (flags, type)
        {
            case (1, 1):
            {
                Check(table, item, cursor, 4);
                var tags = doc.FindTable("VAR");
                var tag = b.D2(item.Offset + cursor);
                cursor += 4;
                return tags is null ? new FunctionArgument(FunctionArgumentKind.Unknown)
                    : new FunctionArgument(FunctionArgumentKind.Tag, Reference: new VarReference(tags.Id, tag));
            }
            case (0, 3):
            {
                Check(table, item, cursor, 4);
                var value = (int)b.D4(item.Offset + cursor);
                cursor += 4;
                return new FunctionArgument(FunctionArgumentKind.Integer, Number: value);
            }
            case (0, 5):
            {
                Check(table, item, cursor, 8);
                var value = b.F64(item.Offset + cursor);
                cursor += 8;
                return new FunctionArgument(FunctionArgumentKind.Real, Real: value);
            }
            case (0, 8):
            {
                Check(table, item, cursor, 4);
                var bytes = b.D4(item.Offset + cursor);
                cursor += 4;
                Check(table, item, cursor, bytes);
                if (bytes < 2 || bytes % 2 != 0 || b.D2(item.Offset + cursor + bytes - 2) != 0)
                {
                    break;
                }
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
