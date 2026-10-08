using FlexRt.Binary;
using FlexRt.Model.Plc.Card;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>Блоки данных карты: объекты с RID <c>0x8a0eNNNN</c> (DB N) и их интерфейсы.</summary>
internal static class CardDbParser
{
    /// <summary>Старшие 16 бит RID объекта DB; младшие — номер DB (наблюдение: 95 DB карты, номера совпали со ссылками панели).</summary>
    private const long DbClass = 0x8a0e;

    /// <summary>Старшие 16 бит RID объекта пользовательского типа (UDT) — наблюдение на двух картах.</summary>
    private const long UdtClass = 0x89fd;

    /// <summary>
    /// Прочитать все DB карты. Сначала читаются интерфейсы и комментарии членов всех блоков
    /// (комментарии члена FB или UDT лежат у FB или UDT), затем у каждого DB строится интерфейс.
    /// Блок не-DB, поток которого не разбирается, даёт сообщение «блок 0x…» в
    /// <see cref="PlcProject.Problems"/>; DB без разобранного интерфейса остаётся в проекте с
    /// <see cref="PlcDb.Problem"/> (см. <see cref="ReadDb"/>).
    /// </summary>
    public static void ReadAll(List<CardStream> streams, long plcId, PlcProject project)
    {
        var blocks = new List<CardBlock>();
        var commentsByName = new Dictionary<string, Dictionary<string, string>>();
        foreach (var group in streams.Where(s => s.Rid != 0).GroupBy(s => s.Rid))
        {
            var block = ReadBlock(group);
            blocks.Add(block);
            if (block.Xml is not null && CardInterfaceBuilder.Name(block.Xml) is { } name)
            {
                commentsByName.TryAdd(name, block.Comments);
            }
            if (block.Problem is not null && !IsDb(block.Rid))
            {
                project.Problems.Add($"блок 0x{block.Rid:x}: {block.Problem}");
            }
        }
        foreach (var block in blocks.Where(b => IsDb(b.Rid)))
        {
            project.Dbs.Add(ReadDb(block, commentsByName, plcId, project));
        }
        foreach (var block in blocks.Where(b => IsUdt(b.Rid) && b.Xml is not null))
        {
            ReadUdt(block, commentsByName, plcId, project);
        }
    }

    /// <summary>
    /// Пользовательский тип: интерфейс из XML (<c>DataTypeSource</c>) в <see cref="PlcProject.Udts"/>
    /// по ПЛК карты и имени, повтор имени — первый. Интерфейс не строится — сообщение «UDT …» в
    /// <see cref="PlcProject.Problems"/>.
    /// </summary>
    private static void ReadUdt(CardBlock block, Dictionary<string, Dictionary<string, string>> commentsByName, long plcId, PlcProject project)
    {
        if (CardInterfaceBuilder.Name(block.Xml!) is not { } name || project.Udts.ContainsKey((plcId, name)))
        {
            return;
        }
        try
        {
            if (CardInterfaceBuilder.Build(block.Rid, block.Xml!, commentsByName, block.Comments, block.Section) is { } udt)
            {
                project.Udts[(plcId, name)] = udt;
            }
        }
        catch (FwxFormatException e)
        {
            project.Problems.Add($"UDT {name}: {e.Message}");
        }
    }

    /// <summary>Блок данных ли объект с этим RID.</summary>
    private static bool IsDb(long rid) => rid >> 16 == DbClass;

    /// <summary>Пользовательский тип (UDT) ли объект с этим RID.</summary>
    private static bool IsUdt(long rid) => rid >> 16 == UdtClass;

    /// <summary>
    /// XML интерфейса блока и комментарии его членов. Интерфейс — первый поток с корнем
    /// <c>&lt;BlockInterface&gt;</c>: тем же словарём сжаты и отладочные данные <c>&lt;DebugInfo&gt;</c>.
    /// Пустые потоки пропускаются, как у комментариев.
    /// </summary>
    /// <returns>
    /// Блок: интерфейс (<c>null</c>, если его нет или поток не разобран), комментарии, прочитанные
    /// до сбоя, и сообщение об ошибке.
    /// </returns>
    private static CardBlock ReadBlock(IGrouping<long, CardStream> streams)
    {
        var section = streams.First().File;
        var comments = new Dictionary<string, string>();
        try
        {
            foreach (var stream in streams.Where(s => s.Kind == CardStreamKind.BlockComments))
            {
                CardXml.AddComments(stream, "Path", comments);
            }
            var intf = streams.Where(s => s.Kind == CardStreamKind.BlockInterface && !string.IsNullOrWhiteSpace(s.Text))
                .Select(CardXml.Parse)
                .FirstOrDefault(x => x.Name.LocalName == "BlockInterface");
            return new CardBlock(streams.Key, section, intf, comments, null);
        }
        catch (FwxFormatException e)
        {
            return new CardBlock(streams.Key, section, null, comments, e.Message);
        }
    }

    /// <summary>
    /// Один DB: номер из RID, имя и интерфейс из XML. Нет интерфейса или ошибка — DB с
    /// <see cref="PlcDb.Problem"/> (сообщение «DB номер (имя): …» в <see cref="PlcProject.Problems"/>):
    /// член такого DB не будет назван отсутствующим.
    /// </summary>
    /// <returns>DB.</returns>
    private static PlcDb ReadDb(CardBlock block, Dictionary<string, Dictionary<string, string>> commentsByName, long plcId, PlcProject project)
    {
        var db = new PlcDb(block.Xml is null ? null : CardInterfaceBuilder.Name(block.Xml), (int)(block.Rid & 0xffff), plcId);
        try
        {
            var root = (block.Xml is null ? null : CardInterfaceBuilder.Build(block.Rid, block.Xml, commentsByName, block.Comments, block.Section))
                ?? throw new FwxFormatException(block.Section, 0, "нет интерфейса");
            db.Interfaces.Add(root);
        }
        catch (FwxFormatException e)
        {
            db.Problem = e.Message;
        }
        // поток блока не разобран: его сообщение уже содержит файл и смещение
        db.Problem = block.Problem ?? db.Problem;
        if (db.Problem is not null)
        {
            project.Problems.Add($"DB {db.Number} ({db.Name ?? "?"}): {db.Problem}");
        }
        return db;
    }
}
