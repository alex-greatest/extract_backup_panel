using FlexRt.Binary;
using FlexRt.Export.Tags;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Строка листа «Теги» <see cref="TagRowFormatter.Format"/>. Ожидания — строки
/// <c>samples/expected/теги.txt</c> и <c>samples/expected/plc/теги.txt</c> и правила листа
/// «Теги» в CLAUDE.md («Установленное поведение»). Документ панели в памяти содержит только
/// соединение: другие его части функция не читает.
/// </summary>
public sealed class TagRowFormatterTests
{
    /// <summary>Текст «данных нет» из CLAUDE.md.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>Текст «тега нет в проекте ПЛК» из CLAUDE.md.</summary>
    private const string NotInPlcFile = "отсутствует в файле ПЛК";

    /// <summary>Id таблицы DATALINK_READWR в файлах TIA (документ таблицы).</summary>
    private const int DatalinkReadWrId = 0x35;

    /// <summary>Внутренний тег: строка <c>A3</c> эталона <c>samples/expected/теги.txt</c>.</summary>
    [Fact]
    public void Format_InternalTag()
    {
        var tag = new HmiTag(2, "Tag_Int", 0x02, 1, 0x106, 0, null, null);

        Assert.Equal(["Tag_Int", "Int", "<Internal tag>", "", "", "", "", "", "", "", ""], TagRowFormatter.Format(Document(), tag, null));
    }

    /// <summary>
    /// Символьный тег, найден в ПЛК: строка <c>A28</c> эталона
    /// <c>samples/expected/plc/теги.txt</c> (<c>Byte_QB2</c> с Source comment).
    /// </summary>
    [Fact]
    public void Format_SymbolicFound()
    {
        var plcTag = new PlcTag("Byte_QB2", 1, "Byte", "%QB2", PlcArea.Output, 0x4b, "Датчик акустики, цех 1");
        var match = new PlcMatch(PlcLookup.Found, "PLC_1", plcTag, PlcLinks.Symbolic(PlcArea.Output, 0x01));

        Assert.Equal(
            ["Byte_QB2", "Byte", "HMI_Connection_1", "PLC_1", "Byte_QB2", "", "<symbolic access>", "1 s", "", "Датчик акустики, цех 1", ""],
            TagRowFormatter.Format(Document(), PlcTag("Byte_QB2", 0x91), match));
    }

    /// <summary>Абсолютный тег, найден в ПЛК: строка <c>A24</c> эталона (<c>Bool_I1_5</c>, <c>%I1.5</c>).</summary>
    [Fact]
    public void Format_AbsoluteFound()
    {
        var plcTag = new PlcTag("Bool_I1_5", 1, "Bool", "%I1.5", PlcArea.Input, 0, "");
        var match = new PlcMatch(PlcLookup.Found, "PLC_1", plcTag, PlcLinks.Absolute(PlcArea.Input, 1, 5, 1, plcTypeCode: 0x07));

        Assert.Equal(
            ["Bool_I1_5", "Bool", "HMI_Connection_1", "PLC_1", "Bool_I1_5", "%I1.5", "<absolute access>", "1 s", "", "", ""],
            TagRowFormatter.Format(Document(), PlcTag("Bool_I1_5", 0x8b), match));
    }

    /// <summary>
    /// Символьный тег не найден: «отсутствует в файле ПЛК» в PLC name, PLC tag и Source
    /// comment (CLAUDE.md); тип — по коду ПЛК из панели.
    /// </summary>
    [Fact]
    public void Format_SymbolicNotInPlcFile()
    {
        var match = new PlcMatch(PlcLookup.NotInPlcFile, "PLC_1", null, PlcLinks.Symbolic(PlcArea.Input, 0x07));

        Assert.Equal(
            ["Bool_I0_0", "Bool", "HMI_Connection_1", NotInPlcFile, NotInPlcFile, "", "<symbolic access>", "1 s", "", NotInPlcFile, ""],
            TagRowFormatter.Format(Document(), PlcTag("Bool_I0_0", 0x8b), match));
    }

    /// <summary>
    /// Абсолютный тег без тега ПЛК: PLC name есть, PLC tag и Source comment пустые (CLAUDE.md).
    /// Имя тега условное: в <c>samples/plc</c> у <c>%IW44</c> тег ПЛК есть, здесь его нет.
    /// </summary>
    [Fact]
    public void Format_AbsoluteAddressOnly()
    {
        var match = new PlcMatch(PlcLookup.AddressOnly, "PLC_1", null, PlcLinks.Absolute(PlcArea.Input, 44, 0, 16, plcTypeCode: 0x03));

        Assert.Equal(
            ["Word_NoPlcTag", "Word", "HMI_Connection_1", "PLC_1", "", "%IW44", "<absolute access>", "1 s", "", "", ""],
            TagRowFormatter.Format(Document(), PlcTag("Word_NoPlcTag", 0x92), match));
    }

    /// <summary>
    /// Нет проекта ПЛК: тип по коду из панели, неразличимые пары через косую черту
    /// (<c>USInt/Char</c>, <c>DInt/Time</c> — CLAUDE.md), колонки ПЛК «неизвестно».
    /// </summary>
    [Theory]
    [InlineData(0x11, "USInt/Char")]
    [InlineData(0x04, "DInt/Time")]
    [InlineData(0x06, "Real")]
    public void Format_NoPlcProject_TypeFromPanelCode(int plcTypeCode, string expectedType)
    {
        var match = new PlcMatch(PlcLookup.Unknown, null, null, PlcLinks.Symbolic(PlcArea.Memory, plcTypeCode));

        Assert.Equal(
            ["T", expectedType, "HMI_Connection_1", Unknown, Unknown, "", "<symbolic access>", "1 s", "", Unknown, ""],
            TagRowFormatter.Format(Document(), PlcTag("T", 0x91), match));
    }

    /// <summary>
    /// Связь не найдена (ссылка на незнакомую таблицу): все колонки, кроме Name, Logged и
    /// Comment, — «неизвестно». Фиксирует текущее поведение, TIA не подтверждено.
    /// </summary>
    [Fact]
    public void Format_NoLink_AllUnknown()
    {
        Assert.Equal(
            ["Screen Number", Unknown, Unknown, Unknown, Unknown, Unknown, Unknown, Unknown, "", Unknown, ""],
            TagRowFormatter.Format(Document(), PlcTag("Screen Number", 0x92), null));
    }

    /// <summary>
    /// Номер соединения вне CONNECTION_OMSP — Connection «неизвестно». Фиксирует текущее
    /// поведение, TIA не подтверждено.
    /// </summary>
    [Fact]
    public void Format_ConnectionOutOfRange_Unknown()
    {
        var match = new PlcMatch(PlcLookup.Unknown, null, null, PlcLinks.Symbolic(PlcArea.Input, 0x07, connection: 1));

        Assert.Equal(Unknown, TagRowFormatter.Format(Document(), PlcTag("T", 0x8b), match)[2]);
    }

    /// <summary>
    /// Цикл опроса (колонка H): 100 ms, 1 s и 10 s — эталон <c>samples/expected/plc/теги.txt</c>;
    /// 500 ms — XML-документация метода (сверено с проектом); 1 min и 1 h — CLAUDE.md;
    /// 0 — пусто (CLAUDE.md).
    /// </summary>
    [Theory]
    [InlineData(100L, "100 ms")]
    [InlineData(500L, "500 ms")]
    [InlineData(1000L, "1 s")]
    [InlineData(10000L, "10 s")]
    [InlineData(60000L, "1 min")]
    [InlineData(3600000L, "1 h")]
    [InlineData(0L, "")]
    public void Format_Cycle(long cycleMs, string expected)
    {
        Assert.Equal(expected, CycleCell(cycleMs));
    }

    /// <summary>
    /// Цикл, не кратный следующей единице, — в меньшей единице (<c>1500 ms</c>, <c>90 s</c>,
    /// <c>2 min</c>). Фиксирует текущее поведение, TIA не подтверждено.
    /// </summary>
    [Theory]
    [InlineData(1500L, "1500 ms")]
    [InlineData(90000L, "90 s")]
    [InlineData(120000L, "2 min")]
    public void Format_Cycle_NotRoundUnits(long cycleMs, string expected)
    {
        Assert.Equal(expected, CycleCell(cycleMs));
    }

    /// <summary>
    /// Тип ПЛК в стиле HMI (XML-документация метода, сверено с Openness-экспортом A603A0097):
    /// <c>String[30]</c> → <c>String</c>, <c>Array[1..1000] of Real</c> →
    /// <c>Array [0..999] of Real</c>; прочее как есть.
    /// </summary>
    [Theory]
    [InlineData("String[30]", "String")]
    [InlineData("Array[1..1000] of Real", "Array [0..999] of Real")]
    [InlineData("LTime_Of_Day", "LTime_Of_Day")]
    public void Format_FoundPlcType_HmiStyle(string plcType, string expected)
    {
        Assert.Equal(expected, FoundTypeCell(plcType));
    }

    /// <summary>
    /// Массив строк и многомерный массив ПЛК: строка внутри массива — <c>String</c>,
    /// многомерный — как есть. Фиксирует текущее поведение, TIA не подтверждено.
    /// </summary>
    [Theory]
    [InlineData("Array[0..9] of String[20]", "Array [0..9] of String")]
    [InlineData("Array[-2..2] of Int", "Array [0..4] of Int")]
    [InlineData("Array[1..2, 1..3] of Int", "Array[1..2, 1..3] of Int")]
    public void Format_FoundPlcType_NestedOrMultiDim(string plcType, string expected)
    {
        Assert.Equal(expected, FoundTypeCell(plcType));
    }

    /// <summary>
    /// Тип по коду ПЛК у массива и строки (XML-документация метода, сверено с
    /// Openness-экспортом A603A0097): массив — <c>Array [0..N-1] of X</c>, строка —
    /// <c>String</c> без длины; неизвестный код — <c>?</c>.
    /// </summary>
    [Theory]
    [InlineData(0x06, 10, "Array [0..9] of Real")]
    [InlineData(0x08, 30, "String")]
    [InlineData(0x0e, 1, "?")]
    public void Format_PanelTypeCode_ArrayAndString(int plcTypeCode, int elements, string expected)
    {
        var match = new PlcMatch(PlcLookup.Unknown, null, null, PlcLinks.Symbolic(PlcArea.DataBlock, plcTypeCode, elements: elements));

        Assert.Equal(expected, TagRowFormatter.Format(Document(), PlcTag("T", 0x84), match)[1]);
    }

    /// <summary>Колонка Acquisition cycle у символьного тега с заданным циклом.</summary>
    /// <param name="cycleMs">Цикл опроса, мс.</param>
    /// <returns>Текст ячейки H.</returns>
    private static string CycleCell(long cycleMs)
    {
        var match = new PlcMatch(PlcLookup.NotInPlcFile, "PLC_1", null, PlcLinks.Symbolic(PlcArea.Memory, 0x05, cycleMs));
        return TagRowFormatter.Format(Document(), PlcTag("T", 0x93), match)[7];
    }

    /// <summary>Колонка Data type у найденного тега ПЛК с заданным типом.</summary>
    /// <param name="plcType">Тип тега ПЛК, как в TIA.</param>
    /// <returns>Текст ячейки B.</returns>
    private static string FoundTypeCell(string plcType)
    {
        var plcTag = new PlcTag("P", 1, plcType, "", PlcArea.DataBlock, 0, "");
        var match = new PlcMatch(PlcLookup.Found, "PLC_1", plcTag, PlcLinks.Symbolic(PlcArea.DataBlock, 0x06));
        return TagRowFormatter.Format(Document(), PlcTag("T", 0x84), match)[1];
    }

    /// <summary>PLC-тег панели со ссылкой на элемент 0 DATALINK_READWR.</summary>
    /// <param name="name">Имя HMI-тега.</param>
    /// <param name="typeCode">Код типа в VAR.</param>
    /// <returns>Тег со связью.</returns>
    private static HmiTag PlcTag(string name, int typeCode) => new(1, name, typeCode, 1, 0, 0, DatalinkReadWrId, 0);

    /// <summary>Документ панели в памяти с одним соединением <c>HMI_Connection_1</c>.</summary>
    /// <returns>Документ без таблиц и тегов.</returns>
    private static FwxDocument Document()
    {
        var header = new FwxHeader(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var doc = new FwxDocument { Binary = new FwxBinary([]), Header = header };
        doc.Connections.Add(new HmiConnection(0, "HMI_Connection_1", 0xc0a80001));
        return doc;
    }
}
