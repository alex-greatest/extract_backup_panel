using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Условия VIEW_DARK и движения VIEW_MOVE для прочитанных I/O field и Button.</summary>
public static class ScreenObjectAnimationParser
{
    /// <summary>Предел вложенности обёрток, как у ScreenObjectParser.</summary>
    private const int MaxDepth = 8;

    /// <summary>Прочитать общие обёртки анимаций после разбора полей и кнопок.</summary>
    public static void Parse(FwxDocument doc)
    {
        var settings = doc.IoFields.Select(field => field.Element).Concat(doc.Buttons.Select(button => button.Element))
            .ToDictionary(element => element, _ => (Conditions: new List<FieldCondition>(), Movements: new List<FieldMovement>()));
        var collected = new Dictionary<VarReference, IReadOnlyList<VarReference>>();
        foreach (var element in doc.Screens.Where(screen => !screen.IsService).SelectMany(screen => screen.Objects))
        {
            Collect(doc, element, settings, collected, 0);
        }
        for (var i = 0; i < doc.IoFields.Count; i++)
        {
            var field = doc.IoFields[i];
            var (conditions, movements) = settings[field.Element];
            doc.IoFields[i] = field with
            {
                Conditions = conditions.Count > 0 ? conditions : field.Conditions,
                Movements = movements.Count > 0 ? movements : field.Movements
            };
        }
        for (var i = 0; i < doc.Buttons.Count; i++)
        {
            var button = doc.Buttons[i];
            var (conditions, movements) = settings[button.Element];
            doc.Buttons[i] = button with { Conditions = conditions, Movements = movements };
        }
    }

    /// <summary>Найти объекты внутри обёртки; сбой её настройки не скрывает объекты от внешних обёрток.</summary>
    /// <returns>Ссылки на поля и кнопки в поддереве.</returns>
    private static IReadOnlyList<VarReference> Collect(FwxDocument doc, VarReference element,
        IReadOnlyDictionary<VarReference, (List<FieldCondition> Conditions, List<FieldMovement> Movements)> settings,
        Dictionary<VarReference, IReadOnlyList<VarReference>> collected, int depth)
    {
        if (settings.ContainsKey(element))
        {
            return [element];
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
            var nested = inner.SelectMany(child => Collect(doc, child, settings, collected, depth + 1)).Distinct().ToArray();
            collected[element] = nested;
            if (nested.Length == 0)
            {
                return nested;
            }
            if (table.Name == "VIEW_DARK" && (links.Count == 0 || end + 8 > item.Length))
            {
                throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: назначение условия поля не прочитано");
            }
            Apply(doc, table, item, links, nested, settings);
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

    /// <summary>Добавить настройки обёртки всем прочитанным объектам внутри неё.</summary>
    private static void Apply(FwxDocument doc, TocEntry table, TableItem item, IReadOnlyList<VarReference> links,
        IReadOnlyList<VarReference> objects,
        IReadOnlyDictionary<VarReference, (List<FieldCondition> Conditions, List<FieldMovement> Movements)> settings)
    {
        if (table.Name != "VIEW_MOVE")
        {
            ApplyConditions(doc, table, item, links, objects, settings);
            return;
        }
        if (FieldMovementReader.Read(doc, item, links) is not { } movement)
        {
            return;
        }
        foreach (var element in objects)
        {
            settings[element].Movements.Add(movement);
        }
    }

    /// <summary>Прочитать условия по порядку; неизвестная роль даёт предупреждение, остальные ссылки продолжаются.</summary>
    private static void ApplyConditions(FwxDocument doc, TocEntry table, TableItem item,
        IReadOnlyList<VarReference> links, IReadOnlyList<VarReference> objects,
        IReadOnlyDictionary<VarReference, (List<FieldCondition> Conditions, List<FieldMovement> Movements)> settings)
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
            foreach (var element in objects)
            {
                settings[element].Conditions.Add(condition);
            }
        }
    }
}
