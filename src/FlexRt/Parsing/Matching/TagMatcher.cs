using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;

namespace FlexRt.Parsing.Matching;

/// <summary>Поиск тегов панели в проекте ПЛК.</summary>
public static class TagMatcher
{
    /// <summary>
    /// Для каждого тега панели (в порядке <see cref="FwxDocument.Tags"/>) найти тег ПЛК.
    /// Внутренний тег — <c>null</c>. У PLC-тега в результат кладётся его связь с ПЛК
    /// (<see cref="PlcMatch.Link"/>). Нет проекта ПЛК — <see cref="PlcLookup.Unknown"/>.
    /// ПЛК выбирается по соединению тега (<see cref="SelectDevice"/>). Символьный тег ищется
    /// по области и ID символа, абсолютный — по адресу; найденный тег другого типа
    /// считается не найденным. Член DB ищется по пути (<see cref="MatchDbMember"/>), член тега
    /// I/Q/M пользовательского типа — в UDT того же ПЛК (<see cref="MatchTagMember"/>).
    /// Абсолютный тег без тега ПЛК с тем же адресом (в том числе любой абсолютный доступ к DB) —
    /// <see cref="PlcLookup.AddressOnly"/>: в TIA HMI-тег может ссылаться прямо на адрес.
    /// Символьный не найден, а в проекте есть неразобранные теги ПЛК —
    /// <see cref="PlcLookup.Unknown"/>: искомый мог быть среди них.
    /// </summary>
    /// <returns>Результаты по тегам панели.</returns>
    public static List<PlcMatch?> Match(FwxDocument doc, PlcProject? project)
    {
        var result = new List<PlcMatch?>();
        foreach (var tag in doc.Tags)
        {
            var link = LinkOf(doc, tag);
            result.Add(tag.LinkTable is null ? null : MatchOne(doc, project, link) with { Link = link });
        }
        return result;
    }

    /// <summary>
    /// Связь тега с ПЛК: элемент DATALINK_READWR или DATALINK (указатель области, например
    /// <c>Screen Number</c>), на который ссылается запись VAR. Id таблиц берутся из TOC, а не
    /// константами.
    /// </summary>
    /// <returns>Связь или <c>null</c>: внутренний тег, ссылка на другую таблицу или индекс вне таблицы.</returns>
    private static PlcLink? LinkOf(FwxDocument doc, HmiTag tag)
    {
        if (tag.LinkTable is not { } table || tag.LinkIndex is not { } index)
        {
            return null;
        }
        var links = table == doc.FindTable("DATALINK_READWR")?.Id ? doc.Links
            : table == doc.FindTable("DATALINK")?.Id ? doc.AreaLinks
            : null;
        return links is not null && index < links.Count ? links[index] : null;
    }

    /// <summary>Найти тег ПЛК для одной связи. Связи нет или она не разобрана — <see cref="PlcLookup.Unknown"/>.</summary>
    /// <returns>Результат поиска.</returns>
    private static PlcMatch MatchOne(FwxDocument doc, PlcProject? project, PlcLink? link)
    {
        if (project is null || link is null || !link.Decoded)
        {
            return new PlcMatch(PlcLookup.Unknown, null, null);
        }
        var device = SelectDevice(doc, project, link.Connection);
        if (device is null)
        {
            return new PlcMatch(PlcLookup.Unknown, null, null);
        }
        if (link.Area == PlcArea.DataBlock)
        {
            // абсолютный доступ к DB (%DB80.DBW0) с членом DB не сопоставляется
            return link.Absolute ? MatchDbAddress(project, device, link) : MatchDbMember(project, device, link);
        }
        if (link is { Absolute: false, Path.Count: > 1 })
        {
            return MatchTagMember(project, device, link);
        }

        var tag = project.Tags.FirstOrDefault(t => t.PlcId == device.Id && IsSameTag(t, link));
        if (tag is not null)
        {
            return new PlcMatch(PlcLookup.Found, device.Name, tag);
        }
        return link.Absolute ? new PlcMatch(PlcLookup.AddressOnly, device.Name, null) : TagNotFound(project, device);
    }

    /// <summary>
    /// Символьный тег не найден: в проекте есть неразобранные теги ПЛК — <see cref="PlcLookup.Unknown"/>
    /// (искомый мог быть среди них), иначе <see cref="PlcLookup.NotInPlcFile"/>.
    /// </summary>
    /// <returns>Результат поиска без тега.</returns>
    private static PlcMatch TagNotFound(PlcProject project, PlcDevice device) =>
        new(project.UnparsedTags > 0 ? PlcLookup.Unknown : PlcLookup.NotInPlcFile, device.Name, null);

    /// <summary>
    /// Член тега I/Q/M пользовательского типа (путь длиннее одного слова): тег — по области и ID
    /// символа из первого слова, тип — UDT того же ПЛК по типу тега (без кавычек), остаток пути —
    /// в интерфейсе типа (<see cref="DbPathResolver.ResolveTagMember"/>). Найден — тег с путём
    /// <c>1.Element_1</c>, типом и комментарием члена. Нет тега — не найден (при неразобранных
    /// тегах ПЛК — <see cref="PlcLookup.Unknown"/>); тип не прочитан — <see cref="PlcLookup.Unknown"/>.
    /// </summary>
    /// <returns>Результат поиска.</returns>
    private static PlcMatch MatchTagMember(PlcProject project, PlcDevice device, PlcLink link)
    {
        var tag = project.Tags.FirstOrDefault(t => t.PlcId == device.Id && t.Area == link.Area && t.SymbolId == link.SymbolId);
        if (tag is null)
        {
            return TagNotFound(project, device);
        }
        if (tag.DataType is not { } type || !TryGetUdt(project, device, type.Trim('"'), out var udt))
        {
            return new PlcMatch(PlcLookup.Unknown, device.Name, null);
        }
        var resolved = DbPathResolver.ResolveTagMember(tag.Name, udt, [.. link.Path.Skip(1)]);
        return MemberMatch(resolved, device, tag.Area, tag.SymbolId);
    }

    /// <summary>UDT по ПЛК тега и имени; нет у этого ПЛК — UDT без найденного владельца (ПЛК 0).</summary>
    /// <returns><c>true</c>, если тип найден.</returns>
    private static bool TryGetUdt(PlcProject project, PlcDevice device, string name, out PlcDbInterface udt) =>
        project.Udts.TryGetValue((device.Id, name), out udt!) || project.Udts.TryGetValue((0, name), out udt!);

    /// <summary>
    /// Абсолютный адрес в DB (<c>%DB1000.DBX0.0</c>): тега ПЛК у такого адреса нет —
    /// <see cref="PlcLookup.AddressOnly"/>. Если DB с этим номером есть в ПЛК, тег результата —
    /// сам DB: его имя в <see cref="PlcTag.Name"/>, тип не задан, комментарий пустой.
    /// </summary>
    /// <returns>Результат поиска.</returns>
    private static PlcMatch MatchDbAddress(PlcProject project, PlcDevice device, PlcLink link)
    {
        var db = project.Dbs.FirstOrDefault(d => d.Number == link.DbNumber && d.PlcId == device.Id && d.Name is not null);
        var tag = db is null ? null : new PlcTag(db.Name!, device.Id, null, "", PlcArea.DataBlock, 0, "");
        return new PlcMatch(PlcLookup.AddressOnly, device.Name, tag);
    }

    /// <summary>
    /// Член DB по символьному пути (<see cref="DbPathResolver"/>). Найден — тег с путём как в TIA
    /// в имени, типом члена (<c>null</c>, если код типа неизвестен) и комментарием члена (пустой, если
    /// его нет); адрес пустой. DB или члена нет — не найден; DB
    /// не прочитан — <see cref="PlcLookup.Unknown"/>.
    /// </summary>
    /// <returns>Результат поиска.</returns>
    private static PlcMatch MatchDbMember(PlcProject project, PlcDevice device, PlcLink link)
    {
        return MemberMatch(DbPathResolver.Resolve(project, device.Id, link), device, PlcArea.DataBlock, 0);
    }

    /// <summary>
    /// Результат поиска члена (DB или тега пользовательского типа) по разрешённому пути. Найден —
    /// тег с путём как в TIA в имени, типом члена (<c>null</c>, если код типа неизвестен),
    /// комментарием члена, заданными областью и ID символа и пустым адресом; не найден — без тега,
    /// с итогом разрешения.
    /// </summary>
    /// <returns>Результат поиска.</returns>
    private static PlcMatch MemberMatch(DbResolution resolved, PlcDevice device, PlcArea? area, long symbolId)
    {
        if (resolved.Lookup != PlcLookup.Found)
        {
            return new PlcMatch(resolved.Lookup, device.Name, null);
        }
        var member = new PlcTag(resolved.Path, device.Id, resolved.Type.Length == 0 ? null : resolved.Type, "", area, symbolId, resolved.Comment);
        return new PlcMatch(PlcLookup.Found, device.Name, member);
    }

    /// <summary>
    /// ПЛК для соединения. В проекте один ПЛК и в панели одно соединение — этот ПЛК, без
    /// проверки IP (у проекта, выгруженного из PLCSIM, IP другой). Иначе — ПЛК, у интерфейса
    /// которого IP соединения; если таких несколько, остаются интерфейсы, подключённые к
    /// подсети; если и тогда не один — ПЛК не выбран.
    /// </summary>
    /// <returns>ПЛК или <c>null</c>.</returns>
    private static PlcDevice? SelectDevice(FwxDocument doc, PlcProject project, int connection)
    {
        if (project.Devices.Count == 1 && doc.Connections.Count == 1)
        {
            return project.Devices[0];
        }
        if (connection >= doc.Connections.Count)
        {
            return null;
        }

        var ip = doc.Connections[connection].Ip;
        var byIp = project.Devices.Where(d => d.Addresses.Any(a => a.Ip == ip)).ToList();
        if (byIp.Count > 1)
        {
            byIp = [.. byIp.Where(d => d.Addresses.Any(a => a.Ip == ip && a.InSubnet))];
        }
        return byIp.Count == 1 ? byIp[0] : null;
    }

    /// <summary>
    /// Тот же ли это тег: символьный — та же область и ID символа, абсолютный — тот же адрес;
    /// и тип тега ПЛК среди возможных по коду связи. Тип не проверяется, если тип тега ПЛК
    /// не найден или не входит в таблицу кодов (строки, массивы, DTL), или код типа в панели
    /// неизвестен (нет в <see cref="PlcTypeCodes"/>).
    /// </summary>
    /// <returns><c>true</c>, если тег совпал.</returns>
    private static bool IsSameTag(PlcTag tag, PlcLink link)
    {
        var sameSymbol = link.Absolute
            ? tag.Address == PlcAddress.Format(link)
            : link.Area is not null && tag.Area == link.Area && tag.SymbolId == link.SymbolId;
        var candidates = PlcTypeCodes.Candidates(link);
        var checkType = tag.DataType is not null && PlcTypeCodes.IsKnown(tag.DataType) && candidates.Count > 0;
        return sameSymbol && (!checkType || candidates.Contains(tag.DataType!));
    }
}
