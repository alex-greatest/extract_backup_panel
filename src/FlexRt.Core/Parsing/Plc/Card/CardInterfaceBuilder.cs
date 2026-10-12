using System.Xml.Linq;
using FlexRt.Binary;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>
/// Интерфейс блока карты (<c>&lt;BlockInterface&gt;</c>) в модели <see cref="PlcDbInterface"/>,
/// той же, что даёт PEData.plf. XML карты — дерево <c>&lt;Part&gt;</c>: у исходной части
/// (<c>DBSource</c>, <c>BlockSource</c> — FB, <c>DataTypeSource</c> — UDT) члены лежат в
/// <c>Payload/Root</c>, вложенные — внутри члена или в части <c>SubParts[SubPartIndex]</c>
/// ближайшей исходной части. Установлено на карте S7-1500 (TIA V19): 441 путь DB из 459 сошлись
/// с именами HMI-тегов панели.
/// </summary>
internal static class CardInterfaceBuilder
{
    /// <summary>Виды исходных частей: у них свой корень членов и свои <c>SubParts</c>.</summary>
    private static readonly string[] SourceKinds = ["DBSource", "BlockSource", "DataTypeSource"];

    /// <summary>Начало имени блока в атрибутах части: <c>BIVE:имя/guid</c> (иногда с <c>_</c> впереди).</summary>
    private const string NameStart = "BIVE:";

    /// <summary>
    /// Имя блока из верхней части: атрибут <c>Block</c>, иначе имя из <c>BIVE:имя/guid</c> в
    /// <c>Info</c> или <c>VersionElement</c>.
    /// </summary>
    /// <returns>Имя или <c>null</c>, если его нет.</returns>
    public static string? Name(XElement blockInterface) => blockInterface.Element("Part") is { } part ? PartName(part) : null;

    /// <summary>
    /// Интерфейс блока. Верхняя часть <c>Values</c> (instance-DB, DB по типу UDT) заменяется
    /// первой исходной частью из её <c>SubParts</c>. Комментарии корня — <paramref name="ownComments"/>,
    /// если исходная часть — свой <c>DBSource</c>, иначе комментарии FB или UDT по имени.
    /// Вложенные FB и UDT собираются в <see cref="PlcDbInterface.Udts"/> по имени (повтор — первый).
    /// </summary>
    /// <returns>Интерфейс или <c>null</c>, если в XML нет части.</returns>
    /// <exception cref="FwxFormatException">Член ссылается на часть, которой нет.</exception>
    public static PlcDbInterface? Build(long rid, XElement blockInterface, IReadOnlyDictionary<string, Dictionary<string, string>> commentsByName,
        Dictionary<string, string> ownComments, string section)
    {
        if (blockInterface.Element("Part") is not { } top)
        {
            return null;
        }
        var source = SourcePart(top);
        var udts = new Dictionary<string, PlcDbInterface>();
        var comments = (string?)source.Attribute("Kind") == "DBSource" ? ownComments : CommentsOf(source, commentsByName);
        var (members, kids) = ReadSource(source, udts, commentsByName, section);
        return new PlcDbInterface(rid, members, kids, udts, comments);
    }

    /// <summary>Исходная часть: сама верхняя или, пока она <c>Values</c>, первая исходная из её <c>SubParts</c>.</summary>
    /// <returns>Часть с членами.</returns>
    private static XElement SourcePart(XElement part)
    {
        while ((string?)part.Attribute("Kind") == "Values" && SubParts(part).FirstOrDefault() is { } first && IsSource(first))
        {
            part = first;
        }
        return part;
    }

    /// <summary>
    /// Члены исходной части: верхние из <c>Payload/Root</c> и вложенные по цепочке ID. Разделы FB
    /// (члены без <c>LID</c>: Input, Output, Static…) дают свои члены под ключом <c>""</c>, как в PEData.plf.
    /// </summary>
    /// <returns>Верхние члены и вложенные по цепочке ID.</returns>
    /// <exception cref="FwxFormatException">Член ссылается на часть, которой нет.</exception>
    private static (List<PlcDbMember> Top, Dictionary<string, List<PlcDbMember>> Kids) ReadSource(
        XElement source, Dictionary<string, PlcDbInterface> udts, IReadOnlyDictionary<string, Dictionary<string, string>> commentsByName, string section)
    {
        var root = source.Element("Payload")?.Element("Root");
        var top = root is null ? [] : MemberElements(root).ToList();
        var kids = new Dictionary<string, List<PlcDbMember>>();
        var walk = new CardInterfaceWalk(kids, udts, commentsByName, section, []);
        AddLevel(walk, source, top, "");
        return ([.. top.Select(PlcDbMemberXml.Read)], kids);
    }

    /// <summary>
    /// Разложить члены уровня: у каждого — вложенные члены внутри элемента, иначе часть по
    /// <c>SubPartIndex</c>: исходная (FB, UDT) идёт в словарь UDT, раздел FB — под ключ <c>""</c>,
    /// прочая (безымянная структура) — под цепочку ID члена. Индекс части берётся в
    /// <c>SubParts</c> <paramref name="owner"/> — ближайшей части, у которой они есть. Раздел FB
    /// (член без <c>LID</c>) кладёт свои члены под <c>""</c>, лежат ли они в части или прямо внутри
    /// элемента раздела (наблюдение: так у системных F-блоков <c>*_C</c>); раздел без части пуст
    /// (у F-блоков пустые разделы опущены, у остальных FB лежат пустой частью).
    /// </summary>
    /// <exception cref="FwxFormatException">Член с <c>LID</c> ссылается на часть, которой нет.</exception>
    private static void AddLevel(CardInterfaceWalk walk, XElement owner, List<XElement> level, string prefix)
    {
        foreach (var member in level)
        {
            var key = member.Attribute("LID") is null ? "" : ChainKey(prefix, member);
            var nested = MemberElements(member).ToList();
            if (nested.Count > 0)
            {
                AddKids(walk, owner, key, nested);
                continue;
            }
            if (!int.TryParse((string?)member.Attribute("SubPartIndex"), out var index))
            {
                continue;
            }
            var part = SubParts(owner).ElementAtOrDefault(index);
            if (part is null && member.Attribute("LID") is not null)
            {
                throw new FwxFormatException(walk.Section, 0, $"член {(string?)member.Attribute("Name")} ссылается на часть {index}, а их {SubParts(owner).Count()}");
            }
            if (part is not null)
            {
                AddPart(walk, owner, part, key);
            }
        }
    }

    /// <summary>Цепочка ID члена: <c>ID</c> на верхнем уровне, иначе <c>префикс:ID</c> (<c>52:51:51</c>).</summary>
    /// <returns>Ключ вложенных членов и комментариев.</returns>
    private static string ChainKey(string prefix, XElement member)
    {
        var id = (string?)member.Attribute("ID");
        return prefix == "" ? id ?? "" : $"{prefix}:{id}";
    }

    /// <summary>
    /// Часть, на которую ссылается член: исходная — в UDT, раздел — под <c>""</c>, структура — под
    /// цепочку ID. Часть, которая уже читается выше по цепочке, — цикл ссылок.
    /// </summary>
    /// <exception cref="FwxFormatException">Член вложенной части ссылается на часть, которой нет, или части ссылаются друг на друга по кругу.</exception>
    private static void AddPart(CardInterfaceWalk walk, XElement owner, XElement part, string key)
    {
        if (IsSource(part))
        {
            AddUdt(walk, part);
            return;
        }
        if (!walk.Ancestors.Add(part))
        {
            throw new FwxFormatException(walk.Section, 0, "части интерфейса ссылаются друг на друга по кругу (SubPartIndex)");
        }
        var nextOwner = SubParts(part).Any() ? part : owner;
        AddKids(walk, nextOwner, key, [.. MemberElements(part)]);
        walk.Ancestors.Remove(part);
    }

    /// <summary>Добавить члены под ключ и разложить их уровень.</summary>
    /// <exception cref="FwxFormatException">Член ссылается на часть, которой нет.</exception>
    private static void AddKids(CardInterfaceWalk walk, XElement owner, string key, List<XElement> members)
    {
        if (!walk.Kids.TryGetValue(key, out var list))
        {
            list = [];
            walk.Kids[key] = list;
        }
        list.AddRange(members.Select(PlcDbMemberXml.Read));
        AddLevel(walk, owner, members, key);
    }

    /// <summary>
    /// Исходная часть FB или UDT как отдельный интерфейс в общем словаре UDT; свои UDT у него пусты,
    /// как в PEData.plf. Безымянная или уже добавленная часть пропускается. На время чтения членов
    /// под именем лежит пустой интерфейс, чтобы повторная ссылка на ту же часть изнутри не читала её снова.
    /// </summary>
    /// <exception cref="FwxFormatException">Член части ссылается на часть, которой нет.</exception>
    private static void AddUdt(CardInterfaceWalk walk, XElement part)
    {
        var name = PartName(part);
        if (name is null || walk.Udts.ContainsKey(name))
        {
            return;
        }
        walk.Udts[name] = UdtInterface([], new Dictionary<string, List<PlcDbMember>>(), new Dictionary<string, string>());
        var (top, kids) = ReadSource(part, walk.Udts, walk.CommentsByName, walk.Section);
        walk.Udts[name] = UdtInterface(top, kids, CommentsOf(part, walk.CommentsByName));
    }

    /// <summary>Интерфейс FB или UDT: ID корня 0 и пустой словарь своих UDT.</summary>
    /// <returns>Интерфейс.</returns>
    private static PlcDbInterface UdtInterface(List<PlcDbMember> top, Dictionary<string, List<PlcDbMember>> kids, IReadOnlyDictionary<string, string> comments)
        => new(0, top, kids, new Dictionary<string, PlcDbInterface>(), comments);

    /// <summary>Комментарии FB или UDT по имени части; нет — пусто.</summary>
    /// <returns>Комментарии по цепочке ID.</returns>
    private static Dictionary<string, string> CommentsOf(XElement part, IReadOnlyDictionary<string, Dictionary<string, string>> commentsByName)
        => PartName(part) is { } name && commentsByName.TryGetValue(name, out var comments) ? comments : new Dictionary<string, string>();

    /// <summary>Члены уровня: элементы <c>&lt;Member&gt;</c> с именем внутри контейнера, через обёртки без имени; <c>SubParts</c> не просматриваются.</summary>
    /// <returns>Элементы членов в порядке XML.</returns>
    private static IEnumerable<XElement> MemberElements(XElement container)
    {
        foreach (var child in container.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "SubParts":
                    continue;
                case "Member" when child.Attribute("Name") is not null:
                    yield return child;
                    continue;
            }
            foreach (var member in MemberElements(child))
            {
                yield return member;
            }
        }
    }

    /// <summary>Имя части: атрибут <c>Block</c>, иначе из <c>BIVE:имя/guid</c> в <c>Info</c> или <c>VersionElement</c>.</summary>
    /// <returns>Имя или <c>null</c>.</returns>
    private static string? PartName(XElement part)
    {
        if ((string?)part.Attribute("Block") is { Length: > 0 } block)
        {
            return block;
        }
        return BiveName((string?)part.Attribute("Info")) ?? BiveName((string?)part.Attribute("VersionElement"));
    }

    /// <summary>Имя из первой строки <c>BIVE:имя/guid</c> в тексте атрибута.</summary>
    /// <returns>Имя или <c>null</c>, если текста нет, в нём нет <c>BIVE:</c> или имя пустое.</returns>
    private static string? BiveName(string? text)
    {
        var start = text?.IndexOf(NameStart, StringComparison.Ordinal) ?? -1;
        if (start < 0)
        {
            return null;
        }
        var nameStart = start + NameStart.Length;
        var slash = text!.IndexOf('/', start);
        return slash > nameStart ? text[nameStart..slash] : null;
    }

    /// <summary>Исходная ли часть: <c>DBSource</c>, <c>BlockSource</c>, <c>DataTypeSource</c>.</summary>
    private static bool IsSource(XElement part) => SourceKinds.Contains((string?)part.Attribute("Kind"));

    /// <summary>Дочерние части <c>SubParts</c>.</summary>
    /// <returns>Части по порядку; индекс — <c>SubPartIndex</c>.</returns>
    private static IEnumerable<XElement> SubParts(XElement part) => part.Element("SubParts")?.Elements("Part") ?? [];
}
