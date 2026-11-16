using System.Numerics;
using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Числовые и ссылочные границы именованного LIMIT; шаги V21 TP700 от 2026-11-16 и A603.</summary>
internal static class TagRangeRecordReader
{
    /// <summary>Длина флагов за ссылками.</summary>
    private const int HeaderSize = 0x08;

    /// <summary>Размер VARIANT постоянной границы.</summary>
    private const int ValueSize = 0x10;

    /// <summary>Маска постоянных границ: Upper 2, Lower 2, Upper 1, Lower 1.</summary>
    private const int AllLimits = 0x0f;

    /// <summary>Прочитать постоянные значения и ссылки, не подменяя ссылочную границу выключенной.</summary>
    /// <returns>Четыре границы Range.</returns>
    /// <exception cref="FwxFormatException">Длина, маска, роль ссылки или значение не разобраны.</exception>
    public static TagRange Read(FwxDocument doc, TableItem item, IReadOnlyList<VarReference> references, HmiTag owner)
    {
        var binary = doc.Binary;
        var body = NamedElement.TryBodyOffset(binary, item);
        if (body is not { } end || end + HeaderSize > item.Length)
        {
            throw new FwxFormatException("LIMIT", item.Offset, "заголовок Range не разобран");
        }
        var mask = binary.D2(item.Offset + end + 0x02);
        if ((mask & ~AllLimits) != 0 || end + HeaderSize + BitOperations.PopCount((uint)mask) * ValueSize != item.Length)
        {
            throw new FwxFormatException("LIMIT", item.Offset + end, $"маска Range 0x{mask:x} или длина значений не разобраны");
        }
        var position = item.Offset + end + HeaderSize;
        Dictionary<int, TagRangeLimit> limits = [];
        // Порядок постоянных значений: Lower 2, Upper 2, Lower 1, Upper 1 (16 сочетаний DInt).
        foreach (var bit in new[] { 0x02, 0x01, 0x08, 0x04 })
        {
            if ((mask & bit) == 0)
            {
                continue;
            }
            limits.Add(bit, new TagRangeLimit(Value(binary, position)));
            position += ValueSize;
        }
        ReadReferences(doc, item, references, owner, end, limits);
        if (limits.Count == 0)
        {
            throw new FwxFormatException("LIMIT", item.Offset, "запись Range не содержит разобранных границ");
        }
        return new TagRange(true, limits.GetValueOrDefault(0x02), limits.GetValueOrDefault(0x01),
            limits.GetValueOrDefault(0x08), limits.GetValueOrDefault(0x04));
    }

    /// <summary>Дополнительные ссылки LIMIT: роли границ и обратная ссылка владельца A603.</summary>
    /// <exception cref="FwxFormatException">Роль не установлена, граница повторяется или ссылка не ведёт на тег.</exception>
    private static void ReadReferences(FwxDocument doc, TableItem item, IReadOnlyList<VarReference> references,
        HmiTag owner, long end, Dictionary<int, TagRangeLimit> limits)
    {
        var start = item.Offset + end - references.Count * 0x08L;
        for (var index = 1; index < references.Count; index++)
        {
            var offset = start + index * 0x08L;
            var word = doc.Binary.D2(offset + 0x04);
            var role = doc.Binary.D2(offset + 0x06);
            var tag = TagRangeReferences.Tag(doc, item, references, index);
            // Diag_SelectIn A603: обратная ссылка на тот же VAR через слова 6, 0x8006.
            if (word == 0x06 && role == 0x8006 && tag.Index == owner.Index)
            {
                continue;
            }
            var bit = role switch
            {
                // Роли проверены по отдельности и с общим тегом всех четырёх границ.
                0x0f => 0x02,
                0x0e => 0x01,
                0xaf => 0x08,
                0xb0 => 0x04,
                _ => 0
            };
            if (word != 0 || bit == 0 || !limits.TryAdd(bit, new TagRangeLimit(Tag: tag.Name)))
            {
                throw new FwxFormatException("LIMIT", offset, $"роль ссылки Range 0x{word:x}/0x{role:x} не разобрана или граница повторяется");
            }
        }
    }

    /// <summary>Постоянная граница i16, i32, f64, u16 или u32; коды проверены реальными шагами.</summary>
    /// <returns>Конечное числовое значение.</returns>
    /// <exception cref="FwxFormatException">Тип, служебные слова или значение не разобраны.</exception>
    private static double Value(FwxBinary binary, long offset)
    {
        if (binary.D2(offset + 0x02) != 0 || binary.D4(offset + 0x04) != 0)
        {
            throw new FwxFormatException("LIMIT", offset, "служебные слова значения Range не разобраны");
        }
        var type = binary.D2(offset);
        var value = type switch
        {
            2 => (short)binary.D2(offset + 0x08),
            3 => (int)binary.D4(offset + 0x08),
            5 => binary.F64(offset + 0x08),
            0x12 => binary.D2(offset + 0x08),
            0x13 => binary.D4(offset + 0x08),
            _ => throw new FwxFormatException("LIMIT", offset, $"тип значения Range 0x{type:x} не разобран")
        };
        return double.IsFinite(value) ? value
            : throw new FwxFormatException("LIMIT", offset, "значение Range не является конечным числом");
    }
}
