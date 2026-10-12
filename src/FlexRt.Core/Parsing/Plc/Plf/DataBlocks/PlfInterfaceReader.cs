using System.Text;
using System.Xml.Linq;
using FlexRt.Binary;
using FlexRt.Model.Plc.Plf;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.Plf.DataBlocks;

/// <summary>
/// Корень интерфейса (объект 0x0022160c) со вложенными членами (0x0022160b) и UDT (корни
/// 0x0022160c), связанными связью 0x0022260f.
/// </summary>
internal static class PlfInterfaceReader
{
    /// <summary>Класс корня интерфейса.</summary>
    public const long RootClass = 0x0022160c;

    /// <summary>Класс вложенных членов.</summary>
    private const long MemberClass = 0x0022160b;

    /// <summary>Тип связи корня с вложенным членом и с UDT.</summary>
    private const long ChildRelation = 0x0022260f;

    /// <summary>Тип связи корня с владельцем интерфейса (FB, DB или UDT).</summary>
    private const long OwnerRelation = 0x00222609;

    /// <summary>Класс объекта UDT — владельца корня интерфейса пользовательского типа.</summary>
    private const long UdtClass = 0x00221003;

    /// <summary>Классы владельцев интерфейса: FB, DB, UDT.</summary>
    private static readonly long[] OwnerClasses = [0x00221001, 0x00221002, UdtClass];

    /// <summary>Начало имени UDT в объекте корня: <c>BIVE:имя/guid</c>.</summary>
    private static readonly byte[] UdtNameStart = [.. "BIVE:"u8];

    /// <summary>Наибольшая длина имени UDT в байтах (наблюдение: имена TIA до 128 символов).</summary>
    private const int MaxUdtNameLength = 0x200;

    /// <summary>
    /// Прочитать корень: члены верхнего уровня из его XML, вложенные члены по <c>ParentId</c>
    /// и, если <paramref name="withUdts"/>, UDT по именам (при повторном имени берётся первый).
    /// Прочитанные корни складываются в <paramref name="cache"/>: UDT и FB общие у многих DB.
    /// </summary>
    /// <returns>Корень интерфейса.</returns>
    /// <exception cref="FwxFormatException">Нет объекта корня или XML корня или члена не разбирается.</exception>
    public static PlcDbInterface Read(PlfFile file, Dictionary<(long, bool), PlcDbInterface> cache, long rootId, bool withUdts)
    {
        if (cache.TryGetValue((rootId, withUdts), out var cached))
        {
            return cached;
        }
        if (!file.TryGet(RootClass, rootId, out var root))
        {
            throw new FwxFormatException(PlfFormat.Section, 0, $"нет корня интерфейса {rootId}");
        }
        var xml = PlfInterfaceXml.Read(file.Binary, root);
        var top = xml is { Name.LocalName: "Root" } ? Members(xml) : [];
        var kids = new Dictionary<string, List<PlcDbMember>>();
        var udts = new Dictionary<string, PlcDbInterface>();
        var relations = PlfRelations.Scan(file, root);
        foreach (var (type, cls, id) in relations)
        {
            if (type != ChildRelation)
            {
                continue;
            }
            switch (cls)
            {
                case MemberClass:
                    AddKids(file.Binary, file.Get(cls, id), kids);
                    break;
                case RootClass when withUdts && UdtName(file.Binary, file.Get(cls, id)) is { } name && !udts.ContainsKey(name):
                    udts[name] = Read(file, cache, id, false);
                    break;
            }
        }
        var result = new PlcDbInterface(rootId, top, kids, udts, ReadComments(file, relations));
        cache[(rootId, withUdts)] = result;
        return result;
    }

    /// <summary>
    /// Пользовательские типы проекта: корни интерфейса, владелец которых (связь 0x222609) — UDT
    /// (класс 0x00221003), по ПЛК владельца и имени из <c>BIVE:имя/guid</c>, со вложенными UDT.
    /// Корень, который не читается, — сообщение «UDT …» в <see cref="PlcProject.Problems"/>,
    /// остальные читаются.
    /// </summary>
    public static void ReadUdts(PlfFile file, PlcProject project)
    {
        var cache = new Dictionary<(long, bool), PlcDbInterface>();
        foreach (var root in file.OfClassById(RootClass))
        {
            try
            {
                AddUdt(file, cache, root, project);
            }
            catch (FwxFormatException e)
            {
                project.Problems.Add($"UDT (корень {root.Id}): {e.Message}");
            }
        }
    }

    /// <summary>
    /// Добавить корень в <see cref="PlcProject.Udts"/>, если его владелец — UDT: ключ — ПЛК владельца
    /// (связь на объект ПЛК, нет — 0) и имя из <c>BIVE:имя/guid</c>; повтор ключа — первый.
    /// </summary>
    /// <exception cref="FwxFormatException">Корень или его члены не читаются.</exception>
    private static void AddUdt(PlfFile file, Dictionary<(long, bool), PlcDbInterface> cache, PlfObject root, PlcProject project)
    {
        var owner = Owner(PlfRelations.Scan(file, root));
        if (owner.Class != UdtClass || UdtName(file.Binary, root) is not { } name)
        {
            return;
        }
        var key = (PlfRelations.PlcId(PlfRelations.Scan(file, file.Get(owner.Class, owner.Id))), name);
        if (!project.Udts.ContainsKey(key))
        {
            project.Udts[key] = Read(file, cache, root.Id, true);
        }
    }

    /// <summary>
    /// Комментарии членов корня: объект 0x0022160d владельца корня (связь 0x222609 корня на FB,
    /// DB или UDT, затем связь 0x222618 владельца). Нет владельца или объекта — комментариев нет.
    /// </summary>
    /// <returns>Непустые комментарии по цепочкам ID.</returns>
    /// <exception cref="FwxFormatException">Объект комментариев не разбирается.</exception>
    private static Dictionary<string, string> ReadComments(PlfFile file, List<PlfRelation> relations)
    {
        var owner = Owner(relations);
        if (owner == default)
        {
            return [];
        }
        var comments = PlfRelations.Find(PlfRelations.Scan(file, file.Get(owner.Class, owner.Id)), PlfMemberComments.Relation, PlfMemberComments.Class);
        return comments == default ? [] : PlfMemberComments.Read(file.Binary, file.Get(comments.Class, comments.Id));
    }

    /// <summary>Владелец корня: первая связь <see cref="OwnerRelation"/> на FB, DB или UDT (<see cref="OwnerClasses"/>).</summary>
    /// <returns>Связь или <c>default</c>, если владельца нет.</returns>
    private static PlfRelation Owner(List<PlfRelation> relations) =>
        relations.FirstOrDefault(r => r.Type == OwnerRelation && OwnerClasses.Contains(r.Class));

    /// <summary>Добавить члены объекта 0x0022160b под его <c>ParentId</c> (нет атрибута — ключ <c>""</c>). Объект без <c>&lt;Member&gt;</c> (начальные значения) пропускается.</summary>
    /// <exception cref="FwxFormatException">XML объекта не разбирается.</exception>
    private static void AddKids(FwxBinary b, PlfObject obj, Dictionary<string, List<PlcDbMember>> kids)
    {
        var xml = PlfInterfaceXml.Read(b, obj);
        if (xml is not { Name.LocalName: "Member" })
        {
            return;
        }
        var key = (string?)xml.Attribute("ParentId") ?? "";
        if (!kids.TryGetValue(key, out var list))
        {
            list = [];
            kids[key] = list;
        }
        list.AddRange(Members(xml));
    }

    /// <summary>Дочерние элементы <c>&lt;Member&gt;</c> как члены (<see cref="PlcDbMemberXml.Read"/>).</summary>
    /// <returns>Члены в порядке XML.</returns>
    private static List<PlcDbMember> Members(XElement parent) => [.. parent.Elements("Member").Select(PlcDbMemberXml.Read)];

    /// <summary>Имя UDT из строки <c>BIVE:имя/guid</c> в объекте корня.</summary>
    /// <returns>Имя или <c>null</c>, если строки нет.</returns>
    private static string? UdtName(FwxBinary b, PlfObject obj)
    {
        var data = b.Span(obj.Offset, obj.Length);
        var start = data.IndexOf(UdtNameStart);
        while (start >= 0)
        {
            var rest = data[(start + UdtNameStart.Length)..];
            var slash = rest.IndexOf((byte)'/');
            if (slash is > 0 and <= MaxUdtNameLength)
            {
                return Encoding.UTF8.GetString(rest[..slash]);
            }
            var next = rest.IndexOf(UdtNameStart);
            if (next < 0)
            {
                return null;
            }
            start += UdtNameStart.Length + next;
        }
        return null;
    }
}
