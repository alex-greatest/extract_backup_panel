using FlexRt.Model.Plc.Card;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Plc.Card;
using Xunit;

namespace FlexRt.Tests.Unit.Plc.Card;

/// <summary>
/// Теги ПЛК карты (<see cref="CardTagParser"/>): таблица <c>&lt;IdentContainer&gt;</c> и
/// комментарии того же файла. XML — строки таблиц карты <c>samples/card/CCB-A03.zip</c>.
/// </summary>
public sealed class CardTagParserTests
{
    /// <summary>Файл таблицы в потоках.</summary>
    private const string TableFile = @"00000003\1\00000036";

    /// <summary>Другой файл таблицы: его комментарии к тегам первой таблицы не относятся.</summary>
    private const string OtherFile = @"00000003\1\0000004D";

    /// <summary>Таблица: бит, байт, слово, двойное слово, UDT; незнакомые область и ширина; тег без имени.</summary>
    private const string Table = """
        <?xml version="1.0" encoding="utf-8"?><IdentContainer>
        <Ident Name="i_040rs1c1_EmS" Scope="Global" LID="225"><SimpleType>Bool</SimpleType><Access SubClass="SimpleAccess"><SimpleAccess BitNumber="3" ByteNumber="13100" Width="Bit" Range="Input" /></Access></Ident>
        <Ident Name="System_Byte" Scope="Global" LID="9"><SimpleType>Byte</SimpleType><Access SubClass="SimpleAccess"><SimpleAccess ByteNumber="1" Width="Byte" Range="Memory" /></Access></Ident>
        <Ident Name="Tag_5" Scope="Global" LID="387"><SimpleType>Word</SimpleType><Access SubClass="SimpleAccess"><SimpleAccess ByteNumber="120" Width="Word" Range="Memory" /></Access></Ident>
        <Ident Name="Counter_In" Scope="Global" LID="400"><SimpleType>DWord</SimpleType><Access SubClass="SimpleAccess"><SimpleAccess ByteNumber="256" Width="DWord" Range="Input" /></Access></Ident>
        <Ident Name="q_-YZ1" Scope="Global" LID="94"><UserDataType>To Fortress</UserDataType><Access SubClass="SimpleAccess"><SimpleAccess ByteNumber="107" Width="Bit" Range="Output" /></Access></Ident>
        <Ident Name="Timer_1" Scope="Global" LID="5"><SimpleType>Bool</SimpleType><Access SubClass="SimpleAccess"><SimpleAccess ByteNumber="1" Width="Bit" Range="Timer" /></Access></Ident>
        <Ident Name="Long_1" Scope="Global" LID="6"><SimpleType>LWord</SimpleType><Access SubClass="SimpleAccess"><SimpleAccess ByteNumber="8" Width="LWord" Range="Input" /></Access></Ident>
        <Ident Scope="Global" LID="7"><SimpleType>Bool</SimpleType><Access SubClass="SimpleAccess"><SimpleAccess ByteNumber="1" Width="Bit" Range="Input" /></Access></Ident>
        </IdentContainer>
        """;

    /// <summary>Комментарии таблицы по <c>RefID</c> = LID.</summary>
    private const string Comments = """
        <?xml version="1.0" encoding="utf-8"?><CommentDictionary><TagLineComments>
        <Comment RefID="225"><DictEntry Language="ru-RU">Аварийный стоп</DictEntry></Comment>
        </TagLineComments></CommentDictionary>
        """;

    /// <summary>Комментарии другого файла с тем же LID, что у тега <c>Tag_5</c>.</summary>
    private const string OtherComments = """
        <?xml version="1.0" encoding="utf-8"?><CommentDictionary><TagLineComments>
        <Comment RefID="387"><DictEntry Language="ru-RU">чужой</DictEntry></Comment>
        </TagLineComments></CommentDictionary>
        """;

    /// <summary>
    /// Адреса как в TIA (бит без <c>BitNumber</c> — 0; <c>B</c>, <c>W</c>, <c>D</c>), UDT в кавычках,
    /// ID символа — LID, комментарий по LID только из своего файла. Незнакомая область, незнакомая
    /// ширина и тег без имени — в проблемы и счётчик неразобранных, остальные читаются.
    /// </summary>
    [Fact]
    public void ReadAll_AddressesTypesCommentsAndProblems()
    {
        var project = new PlcProject();
        List<CardStream> streams =
        [
            new(TableFile, 0x12, 0, CardStreamKind.TagInterface, Table),
            new(TableFile, 0xe0f, 0, CardStreamKind.TagComments, Comments),
            new(OtherFile, 0x596, 0, CardStreamKind.TagComments, OtherComments)
        ];

        CardTagParser.ReadAll(streams, 1, project);

        Assert.Equal(
        [
            new PlcTag("i_040rs1c1_EmS", 1, "Bool", "%I13100.3", PlcArea.Input, 225, "Аварийный стоп"),
            new PlcTag("System_Byte", 1, "Byte", "%MB1", PlcArea.Memory, 9, ""),
            new PlcTag("Tag_5", 1, "Word", "%MW120", PlcArea.Memory, 387, ""),
            new PlcTag("Counter_In", 1, "DWord", "%ID256", PlcArea.Input, 400, ""),
            new PlcTag("q_-YZ1", 1, "\"To Fortress\"", "%Q107.0", PlcArea.Output, 94, "")
        ], project.Tags);
        Assert.Equal(3, project.UnparsedTags);
        Assert.Equal(3, project.Problems.Count);
        Assert.Contains(project.Problems, p => p.Contains("Timer_1"));
        Assert.Contains(project.Problems, p => p.Contains("Long_1"));
        Assert.Contains(project.Problems, p => p.Contains("LID 7"));
    }
}
