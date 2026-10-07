using FlexRt.Binary;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.DataBlocks;

/// <summary>Блоки данных ПЛК из PEData.plf (объекты класса 0x00221002) и деревья их членов.</summary>
internal static class PlfDbReader
{
    /// <summary>Класс объекта DB.</summary>
    private const long DbClass = 0x00221002;

    /// <summary>Класс объекта FB: у instance-DB интерфейс лежит в FB.</summary>
    private const long FbClass = 0x00221001;

    /// <summary>Связь DB с корнем интерфейса: снимок для HMI (предпочтительный).</summary>
    private const long HmiRootRelation = 0x0022260e;

    /// <summary>Связь DB с корнем интерфейса: текущий.</summary>
    private const long CurrentRootRelation = 0x0022260a;

    /// <summary>Связь FB с корнем интерфейса: старый вид (после 0x22260a и 0x22260e).</summary>
    private const long LegacyRootRelation = 0x00222609;

    /// <summary>Связь instance-DB с его FB.</summary>
    private const long FbRelation = 0x0002204c;

    /// <summary>Мусорная пара перед номером DB: 12 байт <c>ff</c>, затем u16 номер и <c>0e 8a</c> (наблюдение).</summary>
    private const int NumberPadding = 12;

    /// <summary>Тип имени DB в объекте: строка <c>03 "DB"</c> перед именем.</summary>
    private static readonly byte[] TypeMark = [0x03, (byte)'D', (byte)'B'];

    /// <summary>Метка после номера DB.</summary>
    private static readonly byte[] NumberMark = [0x0e, 0x8a];

    /// <summary>
    /// Прочитать все DB проекта: имя, номер, ПЛК и корни интерфейса. DB без номера пропускается
    /// и считается в <see cref="PlcProject.UnnumberedDbs"/>: член такого DB нельзя назвать
    /// отсутствующим.
    /// DB, у которого корни не читаются, остаётся в проекте с <see cref="PlcDb.Problem"/>, а
    /// сообщение «DB номер (имя): …» попадает в <see cref="PlcProject.Problems"/>.
    /// </summary>
    public static void Read(PlfFile file, PlcProject project)
    {
        var cache = new Dictionary<(long, bool), PlcDbInterface>();
        foreach (var obj in file.OfClassById(DbClass))
        {
            var number = ReadNumber(file.Binary, obj);
            if (number is null)
            {
                project.UnnumberedDbs++;
                continue;
            }
            var relations = PlfRelations.Scan(file, obj);
            var db = new PlcDb(ReadName(file.Binary, obj), number.Value, PlfRelations.PlcId(relations));
            try
            {
                ReadInterfaces(file, cache, relations, db);
            }
            catch (FwxFormatException e)
            {
                db.Problem = e.Message;
                project.Problems.Add($"DB {db.Number} ({db.Name}): {e.Message}");
            }
            project.Dbs.Add(db);
        }
    }

    /// <summary>
    /// Корни интерфейса DB: собственные (сначала 0x22260e, потом 0x22260a), затем корни FB
    /// по связи 0x2204c (instance-DB). У корня должны читаться члены.
    /// </summary>
    /// <exception cref="FwxFormatException">Корень не читается.</exception>
    private static void ReadInterfaces(PlfFile file, Dictionary<(long, bool), PlcDbInterface> cache, List<PlfRelation> relations, PlcDb db)
    {
        var own = relations.Where(r => r is { Class: PlfInterfaceReader.RootClass, Type: HmiRootRelation or CurrentRootRelation })
            .OrderBy(r => r.Type == HmiRootRelation ? 0 : 1);
        foreach (var root in own)
        {
            AddRoot(file, cache, root.Id, db);
        }
        foreach (var fb in relations.Where(r => r is { Class: FbClass, Type: FbRelation }))
        {
            var roots = PlfRelations.Scan(file, file.Get(fb.Class, fb.Id))
                .Where(r => r is { Class: PlfInterfaceReader.RootClass, Type: HmiRootRelation or CurrentRootRelation or LegacyRootRelation });
            foreach (var root in roots)
            {
                AddRoot(file, cache, root.Id, db);
            }
        }
    }

    /// <summary>Добавить корень интерфейса к DB, если такого корня у него ещё нет (две связи могут вести в один корень).</summary>
    /// <exception cref="FwxFormatException">Корень не читается.</exception>
    private static void AddRoot(PlfFile file, Dictionary<(long, bool), PlcDbInterface> cache, long rootId, PlcDb db)
    {
        if (db.Interfaces.All(i => i.RootId != rootId))
        {
            db.Interfaces.Add(PlfInterfaceReader.Read(file, cache, rootId, true));
        }
    }

    /// <summary>
    /// Номер DB: u16 перед меткой <c>0e 8a</c>, которой предшествуют 12 байт <c>ff</c>
    /// (наблюдение на проекте A603A0097).
    /// </summary>
    /// <returns>Номер или <c>null</c>, если метки нет.</returns>
    private static int? ReadNumber(FwxBinary b, PlfObject obj)
    {
        var data = b.Span(obj.Offset, obj.Length);
        var from = 0;
        while (from < data.Length)
        {
            var i = data[from..].IndexOf(NumberMark);
            if (i < 0)
            {
                return null;
            }
            var k = from + i;
            if (k >= NumberPadding + 2 && data[(k - NumberPadding - 2)..(k - 2)].IndexOfAnyExcept((byte)0xff) < 0)
            {
                return data[k - 2] | (data[k - 1] << 8);
            }
            from = k + 1;
        }
        return null;
    }

    /// <summary>
    /// Имя DB: строка после типа <c>03 "DB"</c>, за ней мультиязычный комментарий; либо
    /// комментарий, затем имя. Ищется первое место, где цепочка сходится.
    /// </summary>
    /// <returns>Имя или <c>null</c>, если не найдено.</returns>
    private static string? ReadName(FwxBinary b, PlfObject obj)
    {
        var end = obj.Offset + obj.Length;
        var data = b.Span(obj.Offset, obj.Length);
        var from = 0;
        while (from < data.Length)
        {
            var i = data[from..].IndexOf(TypeMark);
            if (i < 0)
            {
                return null;
            }
            var pos = obj.Offset + from + i + TypeMark.Length;
            var name = PlfText.Attempt(() => NameThenComment(b, pos, end)) ?? PlfText.Attempt(() => CommentThenName(b, pos, end));
            if (name is not null)
            {
                return name;
            }
            from += i + 1;
        }
        return null;
    }

    /// <summary>Имя, затем мультиязычный комментарий.</summary>
    /// <returns>Имя или <c>null</c>, если имя пустое.</returns>
    /// <exception cref="FwxFormatException">Цепочка не сходится: вызывающий пробует другой порядок.</exception>
    private static string? NameThenComment(FwxBinary b, long pos, long end)
    {
        var (name, next) = PlfText.ReadString(b, pos, end);
        PlfText.ReadMultilingual(b, next, end);
        return name.Length > 0 ? name : null;
    }

    /// <summary>Мультиязычный комментарий, затем имя.</summary>
    /// <returns>Имя или <c>null</c>, если имя пустое.</returns>
    /// <exception cref="FwxFormatException">Цепочка не сходится: вызывающий пробует другой порядок.</exception>
    private static string? CommentThenName(FwxBinary b, long pos, long end)
    {
        var (_, next) = PlfText.ReadMultilingual(b, pos, end);
        var (name, _) = PlfText.ReadString(b, next, end);
        return name.Length > 0 ? name : null;
    }
}
