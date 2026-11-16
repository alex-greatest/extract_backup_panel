using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Ссылки LIMIT на VAR, через ADAPTLOOPBREAKE и через предыдущую ссылку того же элемента.</summary>
internal static class TagRangeReferences
{
    /// <summary>Максимальная длина цепочки ADAPTLOOPBREAKE; известная цепочка A603 содержит одного посредника.</summary>
    private const int MaxDepth = 3;

    /// <summary>Разрешить ссылку владельца или границы в тег; Id 0 означает предыдущую ссылку с указанным индексом.</summary>
    /// <returns>Тег VAR.</returns>
    /// <exception cref="FwxFormatException">Псевдоним, посредник или тег не разобран.</exception>
    public static HmiTag Tag(FwxDocument doc, TableItem item, IReadOnlyList<VarReference> references, int index)
    {
        var link = references[index];
        while (link.Table == 0 && link.Index < index)
        {
            index = link.Index;
            link = references[index];
        }
        for (var depth = 0; depth <= MaxDepth; depth++)
        {
            var table = doc.FindTable(link.Table);
            if (table?.Name == "VAR")
            {
                return doc.Tags.FirstOrDefault(tag => tag.Index == link.Index + 1)
                    ?? throw new FwxFormatException("LIMIT", item.Offset, $"тег VAR {link.Index + 1} не прочитан");
            }
            if (table?.Name != "ADAPTLOOPBREAKE" || depth == MaxDepth)
            {
                break;
            }
            var target = ElementLink.Item(doc, table, link.Index);
            var (_, links) = ReferenceHeader.Read(doc.Binary, target, table.Name);
            if (links.Count == 0)
            {
                break;
            }
            link = links[0];
        }
        throw new FwxFormatException("LIMIT", item.Offset, $"ссылка {index + 1} Range не ведёт на прочитанный тег VAR");
    }
}
