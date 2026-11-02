using System.Text.Json;
using FlexRt.Export.ScreenEvents;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// События экранов Loaded и Cleared (вкладка Events окна, тексты <see cref="ScreenEventTexts"/>): шаговые копии
/// Project1 <c>data/real/steps-events/</c> (вне git) против карты функций из TIA (<c>*.json</c> рядом с копиями),
/// A603A0097 и <c>samples/pdata.fwc</c>, испорченные записи FUNC_LIST и FUNC_*.
/// </summary>
public sealed class ScreenEventsRunTests
{
    /// <summary>
    /// Значения, которые окно сознательно показывает не как TIA (решения пользователя 2026-10-29): объекты проекта —
    /// видом и номером (их таблицы не разобраны), окно Global screen — таблицей и номером (имени в файле нет), имя
    /// программы — в кавычках, как его пишет TIA.
    /// </summary>
    private static readonly Dictionary<string, string> Shown = new()
    {
        ["DataLog_1"] = "журнал #2",
        ["AlarmLog_1"] = "журнал #5",
        ["Data_log_1"] = "журнал #0",
        ["Alarm_log_1"] = "журнал #3",
        ["Report_1"] = "отчёт #0",
        ["Text_list_1"] = "текстовый список #31",
        ["AW1"] = "SCREEN_SYS 7",
        [@"\Storage Card SD\p.exe"] = """
            "\Storage Card SD\p.exe"
            """
    };

    /// <summary>
    /// Каждая функция карты TIA (экран, функция, параметры) — на вкладке Events своего экрана: то же имя (у пар — одно
    /// из имён через « / », у RecipeView — одно), те же имена параметров и значения (кроме <see cref="Shown"/>);
    /// LogTag и GetDataRecordName — отдельно (<see cref="Expected"/>).
    /// </summary>
    [Theory]
    [InlineData("20_all_functions")]
    [InlineData("21_project_objects")]
    [InlineData("22_screen_objects")]
    public void Steps_FunctionsAsInTia(string step)
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.Events, $"{step}.fwc"));
        Assert.Empty(doc.Warnings);
        var texts = new ScreenEventTexts(doc);
        var records = JsonDocument.Parse(File.ReadAllText(StepCopies.Path(StepCopies.Events, $"{step}.json"))).RootElement.EnumerateArray().ToList();
        Assert.NotEmpty(records);

        foreach (var record in records)
        {
            var table = record.GetProperty("table").GetString()!;
            var element = $"{table} {ElementIndex(doc, record, table) + 1}";
            var screen = doc.Screens.Single(s => s.Name == record.GetProperty("screen").GetString());
            var (eventName, function) = texts.Of(screen)
                .SelectMany(e => e.Functions.Select(f => (e.Name, f)))
                .Single(x => x.f.Element == element);
            if (record.TryGetProperty("event", out var expectedEvent))
            {
                Assert.Equal(expectedEvent.GetString(), eventName);
            }
            var name = record.GetProperty("func").GetString()!;
            Assert.True(function.IsRead, element);
            Assert.Contains(name, function.Name.Split(" / "));
            if (name.StartsWith("RecipeView", StringComparison.Ordinal))
            {
                // объект RV1 ссылается на RECIPE_FACTORY — имя одно, без пары SystemDiagnosticView
                Assert.Equal(name, function.Name);
            }
            Assert.Equal(Expected(name, record), function.Parameters.Select(p => $"{p.Name}={p.Value}"));
        }
    }

    /// <summary>
    /// A603A0097 (TP1900 Comfort, V21): 12 экранов с Loaded — Logoff или SetTag, у <c>300_Service</c> — Cleared
    /// Logoff; тег SetTag <c>410…416</c> — через элемент LIMIT <c>Diag_SelectIn~20</c>. Все 13 событий сверены с TIA
    /// (выгрузка экранов через Openness, 2026-10-29).
    /// </summary>
    [Fact]
    public void A603_LoadedAndCleared()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/a603/pdata.fwc"));
        var lines = Lines(doc);

        Assert.Equal(
        [
            "001_AUTO_SCREEN Loaded: 1 Logoff",
            "002_NEXT_STEP Loaded: 1 Logoff",
            "300_Service Cleared: 1 Logoff",
            "410_I_S01 Loaded: 1 SetTag Tag=Diag_SelectIn Value=0",
            "411_I_S02 Loaded: 1 SetTag Tag=Diag_SelectIn Value=1",
            "412_I_S03 Loaded: 1 SetTag Tag=Diag_SelectIn Value=11",
            "413_I_S04 Loaded: 1 SetTag Tag=Diag_SelectIn Value=12",
            "414_I_S05 Loaded: 1 SetTag Tag=Diag_SelectIn Value=13",
            "415_I_S06 Loaded: 1 SetTag Tag=Diag_SelectIn Value=14",
            "416_I_S07 Loaded: 1 SetTag Tag=Diag_SelectIn Value=15",
            "430_O_S01 Loaded: 1 SetTag Tag=Diag_SelectOut Value=0",
            "431_O_S02 Loaded: 1 SetTag Tag=Diag_SelectOut Value=1",
            "432_O_S03 Loaded: 1 SetTag Tag=Diag_SelectOut Value=10"
        ], lines);
        Assert.DoesNotContain(doc.Warnings, w => w.Contains("FUNC_") || w.Contains("ADAPTDEMULTIPLE"));
    }

    /// <summary><c>samples/pdata.fwc</c>: у Root screen Loaded — SetTag Tag_ScreenNumber 1 (как в TIA), Cleared пуст.</summary>
    [Fact]
    public void Sample_RootScreenSetTag()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/pdata.fwc"));

        Assert.Equal(["Root screen Loaded: 1 SetTag Tag=Tag_ScreenNumber Value=1"], Lines(doc));
        Assert.Empty(doc.Warnings);
    }

    /// <summary>
    /// Шаг 24: у <c>Obj_P</c> Loaded — SetTag на член DB (<c>xxxx_2.Element_1</c>, в ПЛК
    /// <c>Data_block_1.ddd.Element_1</c>) со значением 7, как в TIA; без предупреждений.
    /// </summary>
    [Fact]
    public void Step24_DbMemberTag()
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.Events, "24_settag_db_member.fwc"));

        Assert.Empty(doc.Warnings);
        Assert.Contains("Obj_P Loaded: 1 SetTag Tag=xxxx_2.Element_1 Value=7", Lines(doc));
    }

    /// <summary>
    /// Шаг 13 (Screen_2: Loaded SetBit, Cleared SetBit и ResetBit), число ссылок элемента FUNC_LIST Loaded — 0x40:
    /// предупреждение с таблицей и смещением, Loaded «не прочитано», Cleared цел.
    /// </summary>
    [Fact]
    public void BrokenFunctionList_EventNotRead()
    {
        using var dir = new TestDirectory();
        var loaded = LoadedListIndex();
        // +0x04 — u32 число ссылок, см. ReferenceHeader
        var (input, item) = CorruptedCopy.U32(dir, StepThirteen(), "FUNC_LIST", loaded, 0x04, 0x40);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"FUNC_LIST @ 0x{item.Offset:X}: запись {loaded + 1}: 64 ссылок не помещаются в {item.Length} байт"], doc.Warnings);
        var events = new ScreenEventTexts(doc).Of(doc.Screens.Single(s => s.Name == "Screen_2"));
        Assert.False(events[0].IsRead);
        Assert.Equal(["1 SetBit Tag=Bool_I1_5", "2 ResetBit Tag=Bool_I0_0"], events[1].Functions.Select(Line));
    }

    /// <summary>
    /// Шаг 13, первое слово функции SetBit события Cleared (FUNC_BIT 1) — 7: предупреждение с таблицей и смещением, эта
    /// функция «не прочитано», ResetBit того же события и Loaded целы.
    /// </summary>
    [Fact]
    public void BrokenFunction_OnlyItNotRead()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U32(dir, StepThirteen(), "FUNC_BIT", 0, 0x00, 7);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"FUNC_BIT @ 0x{item.Offset:X}: запись 1: первое слово 7, ожидалось 1"], doc.Warnings);
        var events = new ScreenEventTexts(doc).Of(doc.Screens.Single(s => s.Name == "Screen_2"));
        Assert.Equal(["1 SetBit Tag=Bool_I0_0"], events[0].Functions.Select(Line));
        Assert.Equal([$"1 {ScreenEventTexts.NotRead}", "2 ResetBit Tag=Bool_I0_0"], events[1].Functions.Select(Line));
        Assert.Equal("FUNC_BIT 1", events[1].Functions[0].Element);
    }

    /// <summary>
    /// Шаг 20, тип VARIANT параметра ClearAlarmBuffer (<c>Fn_03_04</c>, FUNC_CLEARMSG 1) — 0x11: предупреждение с таблицей,
    /// номером записи и смещением, значение <c>?</c>, функция и остальные экраны читаются.
    /// </summary>
    [Fact]
    public void UnknownVariantType_ParameterUnknown()
    {
        using var dir = new TestDirectory();
        // +0x10 — u16 тип первого блока VARIANT: за u32 1, u32 R = 0, u32 подкодом и u32 видами параметров
        var (input, item) = CorruptedCopy.U16(dir, StepTwenty(), "FUNC_CLEARMSG", 0, 0x10, 0x11);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"FUNC_CLEARMSG: запись 1 @ 0x{item.Offset:X}: тип параметра 1 0x11 (слово 0x0) не известен, параметр не прочитан"], doc.Warnings);
        var lines = Lines(doc);
        Assert.Contains("Fn_03_04 Loaded: 1 ClearAlarmBuffer Alarm class number=?", lines);
        Assert.Contains("Fn_03_05 Loaded: 1 ShowSystemAlarm Text/value=S102_0", lines);
    }

    /// <summary>
    /// Шаг 20, смещение строки параметра ShowSystemAlarm (<c>Fn_03_05</c>, FUNC_SYSMSG 1) — 0xffff: предупреждение с
    /// таблицей и смещением, функция «не прочитано», остальные экраны читаются.
    /// </summary>
    [Fact]
    public void StringOutsideElement_FunctionNotRead()
    {
        using var dir = new TestDirectory();
        // +0x18 — u32 смещение строки в первом блоке VARIANT (блок с +0x10, значение на +8)
        var (input, item) = CorruptedCopy.U32(dir, StepTwenty(), "FUNC_SYSMSG", 0, 0x18, 0xffff);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"FUNC_SYSMSG @ 0x{item.Offset:X}: запись 1: смещение строки 0xffff вне элемента"], doc.Warnings);
        var lines = Lines(doc);
        Assert.Contains($"Fn_03_05 Loaded: 1 {ScreenEventTexts.NotRead}", lines);
        Assert.Contains("Fn_03_04 Loaded: 1 ClearAlarmBuffer Alarm class number=1", lines);
    }

    /// <summary>
    /// Шаг 13, ссылка элемента ADAPTDEMULTIPLE события Cleared <c>Screen_2</c> ведёт на VARSYS: предупреждение с
    /// таблицей и смещением, Cleared «не прочитано», Loaded цел.
    /// </summary>
    [Fact]
    public void ClearedTargetNotFunction_EventNotRead()
    {
        using var dir = new TestDirectory();
        var source = FwxReader.Read(StepCopies.Path(StepCopies.Events, "13_both_events.fwc"));
        var cleared = ListIndex(source, "ADAPTDEMULTIPLE");
        // +0x08 — u16 Id таблицы первой ссылки: за u32 0 и u32 R = 1
        var (input, item) = CorruptedCopy.U16(dir, StepThirteen(), "ADAPTDEMULTIPLE", cleared, 0x08, (ushort)source.FindTable("VARSYS")!.Id);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"ADAPTDEMULTIPLE @ 0x{item.Offset:X}: запись {cleared + 1}: ссылка на VARSYS, ожидалась FUNC_LIST или функция FUNC_*"], doc.Warnings);
        var events = new ScreenEventTexts(doc).Of(doc.Screens.Single(s => s.Name == "Screen_2"));
        Assert.Equal(["1 SetBit Tag=Bool_I0_0"], events[0].Functions.Select(Line));
        Assert.False(events[1].IsRead);
    }

    /// <summary>
    /// Шаг 13, номер элемента в ссылке записи <c>Screen_2</c> на FUNC_LIST — 0x7f (за последним элементом): в
    /// предупреждении — имя экрана, Loaded «не прочитано», Cleared цел.
    /// </summary>
    [Fact]
    public void ListLinkBeyondLast_WarningNamesScreen()
    {
        using var dir = new TestDirectory();
        var source = FwxReader.Read(StepCopies.Path(StepCopies.Events, "13_both_events.fwc"));
        var list = source.FindTable("FUNC_LIST")!;
        var screen = source.Screens.Single(s => s.Name == "Screen_2");
        var (input, _) = CorruptedCopy.U16(dir, StepThirteen(), "SCREEN", screen.Index, ReferenceIndexOffset(screen, list.Id), 0x7f);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"FUNC_LIST @ 0x{list.Offset:X}: экран Screen_2 ссылается на элемент 128, а элементов {list.Entries}"], doc.Warnings);
        var events = new ScreenEventTexts(doc).Of(doc.Screens.Single(s => s.Name == "Screen_2"));
        Assert.False(events[0].IsRead);
        Assert.Equal(["1 SetBit Tag=Bool_I1_5", "2 ResetBit Tag=Bool_I0_0"], events[1].Functions.Select(Line));
    }

    /// <summary>
    /// Смещение u16 номера элемента в ссылке записи SCREEN на таблицу: u16 вид, u16 длина имени, имя с нулями до
    /// кратности 4, u32 R, ссылки по 8 байт (u16 Id таблицы, u16 номер элемента, …).
    /// </summary>
    /// <returns>Смещение от начала записи.</returns>
    private static int ReferenceIndexOffset(Screen screen, int table)
    {
        var references = 4 + (screen.Name.Length * 2 + 3) / 4 * 4 + 4;
        var k = screen.References.Select((r, i) => (r, i)).First(x => x.r.Table == table).i;
        return references + k * 8 + 2;
    }

    /// <summary>Номер элемента с нуля в ссылке записи <c>Screen_2</c> на таблицу события.</summary>
    /// <returns>Номер элемента.</returns>
    private static int ListIndex(FwxDocument doc, string table)
    {
        var id = doc.FindTable(table)!.Id;
        return doc.Screens.Single(s => s.Name == "Screen_2").References.First(r => r.Table == id).Index;
    }

    /// <summary>Шаг 20 — путь относительно корня репозитория; копии нет — тест падает.</summary>
    /// <returns>Путь к копии относительно корня репозитория.</returns>
    private static string StepTwenty()
    {
        const string step = "20_all_functions.fwc";
        StepCopies.Path(StepCopies.Events, step);
        return $"{StepCopies.Events}/{step}";
    }

    /// <summary>Номер элемента функции записи карты: <c>el</c>, у копии 22 его нет — элемент с теми же байтами (<c>hex</c>).</summary>
    /// <returns>Номер элемента с нуля.</returns>
    private static int ElementIndex(FwxDocument doc, JsonElement record, string table)
    {
        if (record.TryGetProperty("el", out var el))
        {
            return el.GetInt32();
        }
        var hex = record.GetProperty("hex").GetString()!.Replace(" ", "");
        return FwxReader.Items(doc.Binary, doc.FindTable(table)!)
            .First(i => Convert.ToHexStringLower(doc.Binary.Span(i.Offset, i.Length)) == hex).Index;
    }

    /// <summary>
    /// Параметры функции, как их показывает окно: <c>имя=значение</c> из карты TIA с заменами <see cref="Shown"/>;
    /// LogTag — оба параметра элемента (номер журнала и ссылка на RECVAR — не разобраны), у GetDataRecordName второй
    /// тег — ссылка на таблицу 0 (нет в TOC, <c>?</c>).
    /// </summary>
    /// <returns>Строки параметров.</returns>
    private static string[] Expected(string function, JsonElement record)
    {
        if (function == "LogTag")
        {
            return ["Параметр 1=объект 0x0 #2", "Tag=RECVAR 4"];
        }
        var parameters = record.GetProperty("params").EnumerateArray()
            .Select(p => (Name: p[0].GetString()!, Value: p[1].GetString()!))
            .Select(p => $"{p.Name}={Shown.GetValueOrDefault(p.Value, p.Value)}")
            .ToArray();
        if (function == "GetDataRecordName")
        {
            parameters[3] = "Data record name=?";
        }
        return parameters;
    }

    /// <summary>События всех экранов с функциями: <c>экран событие: номер имя параметр=значение …</c>, по имени экрана.</summary>
    /// <returns>Строки по событию с функциями.</returns>
    private static List<string> Lines(FwxDocument doc)
    {
        var texts = new ScreenEventTexts(doc);
        return
        [
            .. doc.Screens.Where(s => !s.IsService)
                .SelectMany(s => texts.Of(s).Where(e => e.Functions.Count > 0)
                    .Select(e => $"{s.Name} {e.Name}: {string.Join("; ", e.Functions.Select(Line))}"))
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>Функция одной строкой: номер, имя, параметры <c>имя=значение</c>.</summary>
    /// <returns>Строка.</returns>
    private static string Line(FunctionView function) =>
        string.Join(" ", [$"{function.Number} {function.Name}", .. function.Parameters.Select(p => $"{p.Name}={p.Value}")]);

    /// <summary>Номер элемента FUNC_LIST события Loaded экрана Screen_2 шага 13.</summary>
    /// <returns>Номер с нуля.</returns>
    private static int LoadedListIndex()
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.Events, "13_both_events.fwc"));
        var list = doc.FindTable("FUNC_LIST")!.Id;
        return doc.Screens.Single(s => s.Name == "Screen_2").References.First(r => r.Table == list).Index;
    }

    /// <summary>Шаг 13 — путь относительно корня репозитория; копии нет — тест падает.</summary>
    /// <returns>Путь к копии относительно корня репозитория.</returns>
    private static string StepThirteen()
    {
        const string step = "13_both_events.fwc";
        StepCopies.Path(StepCopies.Events, step);
        return $"{StepCopies.Events}/{step}";
    }
}
