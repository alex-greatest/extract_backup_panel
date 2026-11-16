using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Range тегов по именованным LIMIT; привязка через ссылки на VAR, без зависимости от объектов экрана.</summary>
public static class TagRangeParser
{
    /// <summary>Прочитать именованные LIMIT после VAR; сбой записи сохраняет остальные теги и их Range.</summary>
    public static void Parse(FwxDocument doc)
    {
        if (doc.FindTable("LIMIT") is not { } table)
        {
            return;
        }
        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            if (NamedElement.Kind(doc.Binary, item) == 3)
            {
                ReadRecord(doc, item);
            }
        }
    }

    /// <summary>Владелец записи и четыре границы; при сбое известный владелец получает непрочитанный Range.</summary>
    private static void ReadRecord(FwxDocument doc, TableItem item)
    {
        var tagIndex = -1;
        try
        {
            var references = NamedElement.TryReferences(doc.Binary, item);
            if (references is not { Count: > 0 })
            {
                throw new FwxFormatException("LIMIT", item.Offset, "ссылки владельца Range не разобраны");
            }
            var owner = TagRangeReferences.Tag(doc, item, references, 0);
            tagIndex = doc.Tags.FindIndex(tag => tag.Index == owner.Index);
            var range = TagRangeRecordReader.Read(doc, item, references, owner);
            if (owner.Range is not null && owner.Range != range)
            {
                throw new FwxFormatException("LIMIT", item.Offset, $"разные Range для тега {owner.Name}");
            }
            doc.Tags[tagIndex] = owner with { Range = range };
        }
        catch (FwxFormatException error)
        {
            var who = tagIndex >= 0 ? $"тег {doc.Tags[tagIndex].Name}" : $"запись {item.Index + 1}";
            if (tagIndex >= 0)
            {
                doc.Tags[tagIndex] = doc.Tags[tagIndex] with { Range = new TagRange(false) };
            }
            doc.Warnings.Add($"{error.Message} ({who}: Range не прочитан)");
        }
    }
}
