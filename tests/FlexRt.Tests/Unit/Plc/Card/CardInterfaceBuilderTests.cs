using System.Xml.Linq;
using FlexRt.Binary;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Matching;
using FlexRt.Parsing.Plc.Card;
using Xunit;

namespace FlexRt.Tests.Unit.Plc.Card;

/// <summary>
/// Перевод интерфейса блока карты (<c>&lt;BlockInterface&gt;</c>) в модель
/// <see cref="PlcDbInterface"/> (<see cref="CardInterfaceBuilder"/>). XML — урезанные копии
/// интерфейсов карты <c>samples/card/CCB-A03.zip</c>: та же вложенность частей и атрибуты.
/// </summary>
public sealed class CardInterfaceBuilderTests
{
    /// <summary>Глобальный DB: член-структура ссылается на часть <c>Structure</c> по <c>SubPartIndex</c>.</summary>
    private const string GlobalDb = """
        <BlockInterface><Part Kind="DBSource" VersionElement="BIVE:HMIDataCCD_DB/e344fd63"><Payload><Root>
          <Member ID="2" Name="bReset" RID="0x02000001" LID="9" />
          <Member ID="69" Name="050_Robots" RID="0x92050000" SubPartIndex="0" LID="34" />
        </Root></Payload><SubParts>
          <Part Kind="Structure"><Payload><Member><Member ID="70" Name="StackRbt" RID="0x02000001" LID="15" /></Member></Payload></Part>
        </SubParts></Part></BlockInterface>
        """;

    /// <summary>
    /// Instance-DB: верхняя часть <c>Values</c>, члены — в FB (<c>BlockSource</c>) по разделам;
    /// мультиэкземпляр <c>"MODE"</c> — отдельная исходная часть, раздел Temp опущен (как у F-блоков).
    /// </summary>
    private const string InstanceDb = """
        <BlockInterface><Part Kind="Values" Info="BIVE:090_CtrlDB/2204f6b4"><SubParts>
          <Part Kind="BlockSource" Block="090_CtrlFB"><Payload><Root>
            <Member ID="2" Name="Input" SubPartIndex="0" />
            <Member ID="5" Name="Static" SubPartIndex="1" />
            <Member ID="6" Name="Temp" SubPartIndex="7" />
          </Root></Payload><SubParts>
            <Part Kind="InputSection"><Payload><Member><Member ID="40" Name="Enable" RID="0x02000001" LID="9" /></Member></Payload></Part>
            <Part Kind="StaticSection"><Payload><Member><Member ID="57" Name="ModeMotor" Type="&quot;MODE&quot;" SubPartIndex="2" LID="10" /></Member></Payload></Part>
            <Part Kind="BlockSource" Block="MODE"><Payload><Root><Member ID="5" Name="Static" SubPartIndex="0" /></Root></Payload><SubParts>
              <Part Kind="StaticSection"><Payload><Member><Member ID="129" Name="St" RID="0x02000005" LID="97" /></Member></Payload></Part>
            </SubParts></Part>
          </SubParts></Part>
        </SubParts></Part></BlockInterface>
        """;

    /// <summary>
    /// Глобальный DB: верхние члены, члены структуры под ID члена, имя из <c>BIVE:</c>;
    /// комментарии — свои комментарии DB, а не комментарии блока с тем же именем.
    /// </summary>
    [Fact]
    public void GlobalDb_StructureMembersUnderMemberId()
    {
        var xml = XElement.Parse(GlobalDb);
        var own = new Dictionary<string, string> { ["2"] = "Сброс" };
        var byName = new Dictionary<string, Dictionary<string, string>> { ["HMIDataCCD_DB"] = new() { ["2"] = "чужой" } };

        var root = CardInterfaceBuilder.Build(1, xml, byName, own, "файл")!;

        Assert.Equal("HMIDataCCD_DB", CardInterfaceBuilder.Name(xml));
        Assert.Equal(["bReset", "050_Robots"], root.Top.Select(m => m.Name));
        Assert.Equal([34L], root.Top.Where(m => m.Name == "050_Robots").Select(m => m.Lid));
        Assert.Equal(["StackRbt"], root.Kids["69"].Select(m => m.Name));
        Assert.Equal("Сброс", root.Comments["2"]);
    }

    /// <summary>
    /// Instance-DB: разделы FB без LID сверху, их члены под ключом <c>""</c>, мультиэкземпляр
    /// в словаре UDT по имени; комментарии FB и мультиэкземпляра — у каждого по его имени.
    /// </summary>
    [Fact]
    public void InstanceDb_SectionsAndMultiInstance()
    {
        var xml = XElement.Parse(InstanceDb);
        var byName = new Dictionary<string, Dictionary<string, string>>
        {
            ["090_CtrlFB"] = new() { ["57"] = "Режим" },
            ["MODE"] = new() { ["129"] = "Состояние" }
        };

        var root = CardInterfaceBuilder.Build(1, xml, byName, [], "файл")!;

        Assert.Equal("090_CtrlDB", CardInterfaceBuilder.Name(xml));
        Assert.All(root.Top, m => Assert.Equal(-1, m.Lid));
        Assert.Equal(["Enable", "ModeMotor"], root.Kids[""].Select(m => m.Name));
        Assert.Equal(["St"], root.Udts["MODE"].Kids[""].Select(m => m.Name));
        Assert.Equal("Режим", root.Comments["57"]);
        Assert.Equal("Состояние", root.Udts["MODE"].Comments["129"]);
    }

    /// <summary>
    /// Раздел FB (член без <c>LID</c>) ссылается на часть, которой нет (раздел Temp, <c>SubPartIndex</c> 7
    /// при трёх частях): раздел пуст, ошибки нет — так у системных F-блоков <c>*_C</c>.
    /// </summary>
    [Fact]
    public void InstanceDb_SectionWithoutPart_Empty()
    {
        var root = CardInterfaceBuilder.Build(1, XElement.Parse(InstanceDb), new Dictionary<string, Dictionary<string, string>>(), [], "файл")!;

        Assert.Contains(root.Top, m => m.Name == "Temp");
        Assert.Equal(["Enable", "ModeMotor"], root.Kids[""].Select(m => m.Name));
    }

    /// <summary>
    /// Путь панели по модели с карты: DB1090, LID 10 (мультиэкземпляр) → LID 97 даёт
    /// <c>090_CtrlDB.ModeMotor.St</c> с типом члена <c>Int</c> по RID.
    /// </summary>
    [Fact]
    public void InstanceDb_PanelPathResolves()
    {
        var project = new PlcProject();
        var db = new PlcDb("090_CtrlDB", 1090, 1);
        db.Interfaces.Add(CardInterfaceBuilder.Build(1, XElement.Parse(InstanceDb), new Dictionary<string, Dictionary<string, string>>(), [], "файл")!);
        project.Dbs.Add(db);
        var link = new PlcLink(0, 0, 1000, false, 0x05, PlcArea.DataBlock, 1090, 0, 0, 0, 0, [0x2000000a, 0x40000061], 16, 1, 0, true);

        var resolved = DbPathResolver.Resolve(project, 1, link);

        Assert.Equal(PlcLookup.Found, resolved.Lookup);
        Assert.Equal("090_CtrlDB.ModeMotor.St", resolved.Path);
        Assert.Equal("Int", resolved.Type);
    }

    /// <summary>
    /// Раздел FB с членами прямо внутри элемента раздела (так у системных F-блоков <c>*_C</c>):
    /// члены — под ключом <c>""</c>, их дети — под своим ID без ID раздела.
    /// </summary>
    [Fact]
    public void InlineSectionMembers_UnderEmptyKey()
    {
        const string fb = """
            <BlockInterface><Part Kind="BlockSource" VersionElement="BIVE:F_CTRL1/1"><Payload><Root>
              <Member ID="3" Name="Output" SubPartIndex="1">
                <Member ID="637" Name="QBAD" RID="0x02000001" LID="9" />
                <Member ID="639" Name="Diag" LID="10"><Member ID="640" Name="Code" RID="0x02000004" LID="9" /></Member>
              </Member>
            </Root></Payload></Part></BlockInterface>
            """;

        var root = CardInterfaceBuilder.Build(1, XElement.Parse(fb), new Dictionary<string, Dictionary<string, string>>(), [], "файл")!;

        Assert.Equal(["QBAD", "Diag"], root.Kids[""].Select(m => m.Name));
        Assert.Equal(["Code"], root.Kids["639"].Select(m => m.Name));
    }

    /// <summary>Структура ссылается сама на себя через <c>SubPartIndex</c>: ошибка формата, а не бесконечная рекурсия.</summary>
    [Fact]
    public void SelfReferencingPart_Throws()
    {
        const string db = """
            <BlockInterface><Part Kind="DBSource" VersionElement="BIVE:DbLoop/1"><Payload><Root>
              <Member ID="2" Name="S" SubPartIndex="0" LID="9" />
            </Root></Payload><SubParts>
              <Part Kind="Structure"><Payload><Member><Member ID="3" Name="Again" SubPartIndex="0" LID="9" /></Member></Payload></Part>
            </SubParts></Part></BlockInterface>
            """;

        var error = Assert.Throws<FwxFormatException>(() =>
            CardInterfaceBuilder.Build(1, XElement.Parse(db), new Dictionary<string, Dictionary<string, string>>(), [], "файл"));

        Assert.Contains("по кругу", error.Message);
    }

    /// <summary>Член с LID ссылается на часть, которой нет: ошибка формата с файлом блока, именем члена и номером части.</summary>
    [Fact]
    public void MissingStructurePart_Throws()
    {
        var xml = XElement.Parse(GlobalDb.Replace("SubPartIndex=\"0\"", "SubPartIndex=\"3\""));

        var error = Assert.Throws<FwxFormatException>(() =>
            CardInterfaceBuilder.Build(1, xml, new Dictionary<string, Dictionary<string, string>>(), [], @"00000A0E\1\0000002F"));

        Assert.Equal(@"00000A0E\1\0000002F", error.Section);
        Assert.Contains("член 050_Robots ссылается на часть 3", error.Message);
    }
}
