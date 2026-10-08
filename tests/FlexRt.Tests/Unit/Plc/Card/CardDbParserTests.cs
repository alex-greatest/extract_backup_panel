using FlexRt.Model.Plc.Card;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Plc.Card;
using Xunit;

namespace FlexRt.Tests.Unit.Plc.Card;

/// <summary>
/// Блоки данных карты (<see cref="CardDbParser"/>): номер из RID, интерфейс, комментарии FB у
/// instance-DB и поведение на неразобранных блоках. XML — урезанные интерфейсы карты
/// <c>samples/card/CCB-A03.zip</c>.
/// </summary>
public sealed class CardDbParserTests
{
    /// <summary>Файл блоков в потоках.</summary>
    private const string BlockFile = @"00000A0E\1\0000002F";

    /// <summary>Глобальный DB1 с одним членом.</summary>
    private const string GlobalDb = """
        <BlockInterface><Part Kind="DBSource" VersionElement="BIVE:HMIDataCCD_DB/e344fd63"><Payload><Root>
          <Member ID="2" Name="bReset" RID="0x02000001" LID="9" />
        </Root></Payload></Part></BlockInterface>
        """;

    /// <summary>FB, интерфейс которого берёт instance-DB.</summary>
    private const string Fb = """
        <BlockInterface><Part Kind="BlockSource" VersionElement="BIVE:090_CtrlFB/2b9bf829"><Payload><Root>
          <Member ID="5" Name="Static" SubPartIndex="0" />
        </Root></Payload><SubParts>
          <Part Kind="StaticSection"><Payload><Member><Member ID="57" Name="Run" RID="0x02000001" LID="10" /></Member></Payload></Part>
        </SubParts></Part></BlockInterface>
        """;

    /// <summary>Instance-DB1090 этого FB: члены — в копии интерфейса FB внутри своего XML.</summary>
    private const string InstanceDb = """
        <BlockInterface><Part Kind="Values" Info="BIVE:090_CtrlDB/2204f6b4"><SubParts>
          <Part Kind="BlockSource" Block="090_CtrlFB"><Payload><Root><Member ID="5" Name="Static" SubPartIndex="0" /></Root></Payload><SubParts>
            <Part Kind="StaticSection"><Payload><Member><Member ID="57" Name="Run" RID="0x02000001" LID="10" /></Member></Payload></Part>
          </SubParts></Part>
        </SubParts></Part></BlockInterface>
        """;

    /// <summary>Комментарии FB.</summary>
    private const string FbComments = """<InterfaceLineComments><Part Kind="Comments"><Comment Path="57"><DictEntry Language="ru-RU">Пуск</DictEntry></Comment></Part></InterfaceLineComments>""";

    /// <summary>
    /// Номер DB из RID, имя и интерфейс из потока с корнем <c>BlockInterface</c> (поток
    /// <c>DebugInfo</c> тем же словарём пропускается); комментарий члена instance-DB — от FB по имени. DB с битым
    /// XML и DB без интерфейса остаются в проекте с проблемой «DB номер (имя)»; блок кода с
    /// битым XML — сообщение «блок 0x…». Остальные DB читаются.
    /// </summary>
    [Fact]
    public void ReadAll_DbsCommentsAndProblems()
    {
        var project = new PlcProject();
        List<CardStream> streams =
        [
            new(BlockFile, 0x08, 0x8a0e0001, CardStreamKind.BlockInterface, "<DebugInfo />"),
            new(BlockFile, 0x10, 0x8a0e0001, CardStreamKind.BlockInterface, GlobalDb),
            new(BlockFile, 0x20, 0x8a130442, CardStreamKind.BlockInterface, Fb),
            new(BlockFile, 0x30, 0x8a130442, CardStreamKind.BlockComments, FbComments),
            new(BlockFile, 0x40, 0x8a0e0442, CardStreamKind.BlockInterface, InstanceDb),
            new(BlockFile, 0x50, 0x8a0e0002, CardStreamKind.BlockInterface, "<BlockInterface>"),
            new(BlockFile, 0x60, 0x8a0e0003, CardStreamKind.BlockComments, ""),
            new(BlockFile, 0x70, 0x8a120005, CardStreamKind.BlockInterface, "<BlockInterface>")
        ];

        CardDbParser.ReadAll(streams, 1, project);

        Assert.Equal([1, 1090, 2, 3], project.Dbs.Select(d => d.Number));
        var global = project.Dbs.Single(d => d.Number == 1);
        Assert.Equal("HMIDataCCD_DB", global.Name);
        Assert.Equal(["bReset"], global.Interfaces.Single().Top.Select(m => m.Name));
        var instance = project.Dbs.Single(d => d.Number == 1090);
        Assert.Equal("090_CtrlDB", instance.Name);
        Assert.Equal("Пуск", instance.Interfaces.Single().Comments["57"]);
        Assert.StartsWith($"{BlockFile} @ 0x50: XML не разбирается", project.Dbs.Single(d => d.Number == 2).Problem);
        Assert.Contains("нет интерфейса", project.Dbs.Single(d => d.Number == 3).Problem);
        Assert.Contains(project.Problems, p => p.StartsWith("DB 2 ("));
        Assert.Contains(project.Problems, p => p.StartsWith("DB 3 ("));
        Assert.Contains(project.Problems, p => p.StartsWith("блок 0x8a120005"));
        Assert.Equal(3, project.Problems.Count);
    }
}
