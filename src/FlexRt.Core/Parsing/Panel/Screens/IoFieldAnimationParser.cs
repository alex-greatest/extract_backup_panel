using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Анимации обёрток I/O field: условия VIEW_DARK и движения VIEW_MOVE; после IoFieldParser.</summary>
public static class IoFieldAnimationParser
{
    /// <summary>Предел вложенности обёрток, как у ScreenObjectParser.</summary>
    private const int MaxDepth = 8;

    /// <summary>Прочитать анимации только обёрток, содержащих распознанные поля; общая обёртка читается один раз.</summary>
    public static void Parse(FwxDocument doc)
    {
        var fields = doc.IoFields.Select((field, index) => (field.Element, Index: index)).ToDictionary(f => f.Element, f => f.Index);
        var collected = new Dictionary<VarReference, IReadOnlyList<int>>();
        foreach (var element in doc.Screens.Where(s => !s.IsService).SelectMany(s => s.Objects))
        {
            Collect(doc, element, fields, collected, 0);
        }
    }

    /// <summary>Найти поля внутри обёртки; сбой её настройки не скрывает найденные поля от внешних обёрток.</summary>
    /// <returns>Индексы полей документа в поддереве.</returns>
    private static IReadOnlyList<int> Collect(FwxDocument doc, VarReference element, IReadOnlyDictionary<VarReference, int> fields,
        Dictionary<VarReference, IReadOnlyList<int>> collected, int depth)
    {
        if (fields.TryGetValue(element, out var index))
        {
            return [index];
        }
        if (collected.TryGetValue(element, out var previous))
        {
            return previous;
        }
        collected[element] = [];
        if (depth > MaxDepth || doc.FindTable(element.Table) is not { Name: "VIEW_DARK" or "VIEW_MOVE" } table)
        {
            return [];
        }
        try
        {
            var item = ElementLink.Item(doc, table, element.Index);
            var (end, links) = ReferenceHeader.Read(doc.Binary, item, table.Name);
            var inner = Inner(doc, table, item, end);
            var nested = inner.SelectMany(child => Collect(doc, child, fields, collected, depth + 1)).Distinct().ToArray();
            collected[element] = nested;
            if (nested.Length == 0)
            {
                return nested;
            }
            if (table.Name == "VIEW_DARK" && (links.Count == 0 || end + 8 > item.Length))
            {
                throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: назначение условия поля не прочитано");
            }
            Apply(doc, table, item, links, nested);
            return nested;
        }
        catch (FwxFormatException e)
        {
            if (!doc.Warnings.Contains(e.Message))
            {
                doc.Warnings.Add(e.Message);
            }
            return collected[element];
        }
    }

    /// <summary>Пары объектов обёртки: одна у VIEW_DARK, список после двух u16 у VIEW_MOVE.</summary>
    /// <returns>Ссылки на дочерние объекты.</returns>
    /// <exception cref="FwxFormatException">Пары не помещаются в элемент.</exception>
    private static List<VarReference> Inner(FwxDocument doc, TocEntry table, TableItem item, long end)
    {
        if (table.Name == "VIEW_MOVE" && end + 4 > item.Length)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: число объектов не помещается в {item.Length} байт");
        }
        var count = table.Name == "VIEW_DARK" ? 1 : doc.Binary.D2(item.Offset + end + 2);
        var offset = table.Name == "VIEW_DARK" ? end : end + 4;
        return offset + count * (long)ObjectPairs.Size <= item.Length
            ? ObjectPairs.Read(doc.Binary, item.Offset + offset, count)
            : throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: {count} объектов не помещаются в {item.Length} байт");
    }

    /// <summary>Добавить условие либо движение всем полям внутри обёртки; остальные свойства поля сохраняются.</summary>
    private static void Apply(FwxDocument doc, TocEntry table, TableItem item, IReadOnlyList<VarReference> links, IReadOnlyList<int> fields)
    {
        if (table.Name == "VIEW_MOVE")
        {
            ApplyMovement(doc, item, links, fields);
            return;
        }
        ApplyConditions(doc, table, item, links, fields);
    }

    /// <summary>Прочитать движение обёртки и добавить его каждому вложенному полю.</summary>
    private static void ApplyMovement(FwxDocument doc, TableItem item, IReadOnlyList<VarReference> links, IReadOnlyList<int> fields)
    {
        if (FieldMovementReader.Read(doc, item, links) is not { } movement)
        {
            return;
        }
        foreach (var index in fields)
        {
            var field = doc.IoFields[index];
            doc.IoFields[index] = field with { Movements = [.. field.Movements ?? [], movement] };
        }
    }

    /// <summary>Прочитать условия по порядку; неизвестная роль даёт предупреждение, остальные ссылки продолжаются.</summary>
    private static void ApplyConditions(FwxDocument doc, TocEntry table, TableItem item,
        IReadOnlyList<VarReference> links, IReadOnlyList<int> fields)
    {
        for (var i = 0; i < links.Count; i++)
        {
            // Второе служебное слово: 0 — Visibility, 0x19 — Control enable; оба в одной обёртке на шаге 25.
            var role = doc.Binary.D2(item.Offset + 0x0e + i * 8L);
            if (role is not (0 or 0x19))
            {
                doc.Warnings.Add(new FwxFormatException(table.Name, item.Offset,
                    $"запись {item.Index + 1}: назначение ссылки условия 0x{role:X} не разобрано").Message);
                continue;
            }
            var condition = FieldConditionReader.Read(doc, links[i],
                role == 0x19 ? FieldConditionKind.ControlEnable : FieldConditionKind.Visibility, item.Offset + 0x08 + i * 8L);
            foreach (var index in fields)
            {
                var field = doc.IoFields[index];
                doc.IoFields[index] = field with { Conditions = [.. field.Conditions ?? [], condition] };
            }
        }
    }
}
