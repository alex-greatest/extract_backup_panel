using System.Buffers.Binary;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Интеграционные тесты на отсутствующий и испорченный вход. Испорченные файлы — копии
/// реальных <c>samples/</c>, изменённые в рантайме во временном каталоге теста
/// (<see cref="TestDirectory"/>); синтетических файлов нет, <c>samples/</c> не меняется.
/// Ожидания — установленное поведение в <c>.claude/rules/</c> (разбор таблиц — <c>формат-панели.md</c>,
/// алармы — <c>алармы.md</c>, экраны и картинки — <c>экраны-и-картинки.md</c>).
/// </summary>
public sealed partial class DamagedInputTests
{
    /// <summary>Текст ячейки ПЛК, если данных нет: нет файла ПЛК или он сломан.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>Размер заголовка <c>PEData.plf</c>; с него начинается первый кадр (<c>docs/формат-plf/общая-структура.md</c>).</summary>
    private const int PlfHeaderSize = 0x62;

    /// <summary>
    /// Строка <c>Char_IB3</c> (символьный доступ, код ПЛК 0x11) без данных ПЛК: тип по коду
    /// панели — неразличимая пара <c>USInt/Char</c>, колонки ПЛК «неизвестно», адреса нет.
    /// </summary>
    private const string CharIb3Row =
        "A39=Char_IB3 | B39=USInt/Char | C39=HMI_Connection_1 | D39=неизвестно | E39=неизвестно | G39=<symbolic access> | H39=1 s | J39=неизвестно";

    /// <summary>
    /// Строка <c>DInt_ID6</c> (абсолютный доступ, код ПЛК 0x04) без данных ПЛК: тип
    /// <c>DInt/Time</c>, адрес из панели <c>%ID6</c>, колонки ПЛК «неизвестно».
    /// </summary>
    private const string DIntId6Row =
        "A44=DInt_ID6 | B44=DInt/Time | C44=HMI_Connection_1 | D44=неизвестно | E44=неизвестно | F44=%ID6 | G44=<absolute access> | H44=1 s | J44=неизвестно";

    /// <summary>Адрес ячейки в начале ячейки слепка: <c>A12=</c>.</summary>
    [GeneratedRegex(@"(?<=^| \| )([A-Z]+)(\d+)=")]
    private static partial Regex CellAddress();

    /// <summary>Строка статистики «Всего тегов» или «Внутренних тегов» и её число: <c>A1=Всего тегов | B1=16</c>.</summary>
    [GeneratedRegex(@"^(A[12]=(?:Всего|Внутренних) тегов \| B[12]=)(\d+)$")]
    private static partial Regex StatisticsMinusOne();

    /// <summary>
    /// <c>samples/plc/pdata.fwc</c> (два соединения) без файла ПЛК: без предупреждений, строка «не найден», у
    /// PLC-тегов колонки ПЛК «неизвестно», тип — по коду панели (<c>USInt/Char</c>, <c>DInt/Time</c>);
    /// 645 строк языков, разбиение таблиц совпадает с эталоном — без данных ПЛК такая панель читается.
    /// </summary>
    [Fact]
    public void PlcPanel_WithoutPlcFile_PlcColumnsUnknown()
    {
        using var dir = new TestDirectory();
        var plc = dir.MissingFile();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/plc/pdata.fwc"), plc, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Тегов: 69");
        RunAssertions.HasLine(run.Output, $"Файл ПЛК: {plc} не найден - данные ПЛК: неизвестно");
        AssertPlcColumnsUnknown(dir.PanelDataWorkbook);
        AssertRow(dir.PanelDataWorkbook, CharIb3Row);
        AssertRow(dir.PanelDataWorkbook, DIntId6Row);
        AssertRow(dir.PanelDataWorkbook, "A4=Найдено в ПЛК | B4=0");
        AssertRow(dir.PanelDataWorkbook, "A6=Данные ПЛК неизвестны | B6=50");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, $"Данные ПЛК=не найден: {plc}");
        RunAssertions.StringsSheet(PanelRunner.SaveStrings(run, dir), 645);
        RunAssertions.TablesMatch("samples/plc/pdata.fwc", "samples/expected/plc/pdata.sha256");
    }

    /// <summary>
    /// Копия <c>samples/plc/PEData.plf</c> с инвертированным байтом в середине данных первого
    /// кадра, панель <c>samples/card/pdata.fwc</c> (одно соединение): SHA-256 кадра не совпадает.
    /// Предупреждение в тексте запуска, колонки ПЛК «неизвестно».
    /// </summary>
    [Fact]
    public void BrokenPlcFrame_WarnsAndPlcColumnsUnknown()
    {
        using var dir = new TestDirectory();
        var plc = dir.CopyFromRepo("samples/plc/PEData.plf", "PEData.plf");
        var bytes = File.ReadAllBytes(plc);
        // кадр: [u32 size][данные][ff][SHA-256]; size считает поле длины, данные и ff,
        // поэтому данные — [0x66, 0x62 + size - 2]; середина кадра лежит внутри них
        var size = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(PlfHeaderSize));
        bytes[PlfHeaderSize + size / 2] ^= 0xff;
        File.WriteAllBytes(plc, bytes);

        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/card/pdata.fwc"), plc, dir.OutDir);

        var message = $"{plc}: PEData.plf @ 0x{PlfHeaderSize:X}: контрольная сумма кадра не совпала";
        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {message}");
        RunAssertions.HasLine(run.Output, $"Файл ПЛК: {plc} не прочитан - данные ПЛК: неизвестно");
        RunAssertions.NoStackTrace(run);
        SummarySheetDump.Contains(dir.PanelDataWorkbook, $"Данные ПЛК=не прочитан: {plc}");
        AssertPlcColumnsUnknown(dir.PanelDataWorkbook);
    }

    /// <summary>
    /// Копия <c>samples/pdata.fwc</c>, в которой у внутреннего тега из середины таблицы VAR первое
    /// слово 4 вместо 3. Смещение записи — из разбора той же копии через
    /// <see cref="FwxReader"/> (TOC → каталог смещений VAR). Сводное предупреждение
    /// VAR с номером записи и смещением, тегов на один меньше, остальные строки листа «Теги»
    /// — как в эталоне. Строка тега в эталоне ищется по имени, а не по номеру записи, а тег берётся внутренний,
    /// поэтому «Внутренних тегов» в ожидании честно на 1 меньше.
    /// </summary>
    [Fact]
    public void BrokenVarRecord_SkippedWithWarning()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo("samples/pdata.fwc", "pdata.fwc");
        var tags = FwxReader.Read(input).Tags;
        var internalTags = tags.Where(t => t.LinkTable is null).ToList();
        var broken = internalTags[internalTags.Count / 2];
        var bytes = File.ReadAllBytes(input);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)broken.Offset), 4);
        File.WriteAllBytes(input, bytes);

        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Output, $"Тегов: {tags.Count - 1}");
        RunAssertions.HasLine(run.Errors,
            $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - VAR: пропущено записей с незнакомой раскладкой: 1, первая — VAR @ 0x{broken.Offset:X}: запись {broken.Index}: первое слово 0x4, ожидалось 0x3");
        RunAssertions.NoStackTrace(run);
        var lines = File.ReadAllLines(RepositoryPaths.InRepo("samples/expected/теги.txt"));
        var row = RowOf(Assert.Single(lines, l => l.Split(" | ")[0].EndsWith($"={broken.Name}", StringComparison.Ordinal) && l.StartsWith('A')));
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, WithoutRow(lines, row), $"samples/expected/теги.txt без строки {row} ({broken.Name}), всего и внутренних на 1 меньше");
    }

    /// <summary>
    /// Копия <c>samples/pdata.fwc</c>, в которой число событий SYSMSGHANDLER (+0x2a) — 0xffff:
    /// список не помещается в запись. Предупреждение с таблицей и смещением записи,
    /// «Системных событий: 0», листа «Системные события» нет, лист «Теги» — как в эталоне.
    /// </summary>
    [Fact]
    public void BrokenSysMsgHandler_WarnsAndNoEventsSheet()
    {
        using var dir = new TestDirectory();
        // +0x2a — u16 число событий, см. SysMsgHandlerParser
        var (input, item) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "SYSMSGHANDLER", 0, 0x2a, 0xffff);

        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Output, "Системных событий: 0");
        RunAssertions.HasLine(run.Errors,
            $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SYSMSGHANDLER @ 0x{item.Offset:X}: запись 1: 65535 событий не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        SheetDump.Absent(dir.PanelDataWorkbook, "Системные события");
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/теги.txt");
    }

    /// <summary>
    /// Копия <c>samples/pdata.fwc</c>, в которой смещение единственной записи SYSMSGHANDLER в
    /// каталоге сдвинуто так, что запись — последние 0x10 байт таблицы, короче заголовка 0x2c.
    /// Предупреждение с таблицей и смещением записи, листа «Системные события» нет.
    /// </summary>
    [Fact]
    public void ShortSysMsgHandlerRecord_WarnsAndNoEventsSheet()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo("samples/pdata.fwc", "pdata.fwc");
        var doc = FwxReader.Read(input);
        var table = doc.FindTable("SYSMSGHANDLER")!;
        var item = FwxReader.Items(doc.Binary, table).Single();
        const int shortLength = 0x10;
        var bytes = File.ReadAllBytes(input);
        // каталог смещений — сразу за заголовком таблицы 0x34; смещение — от начала блока данных
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)table.Offset + FwxReader.TableHeaderSize), (uint)(item.Length - shortLength));
        File.WriteAllBytes(input, bytes);

        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Output, "Системных событий: 0");
        RunAssertions.HasLine(run.Errors,
            $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SYSMSGHANDLER @ 0x{item.Offset + item.Length - shortLength:X}: запись 1: длина {shortLength} меньше заголовка 0x2c");
        RunAssertions.NoStackTrace(run);
        SheetDump.Absent(dir.PanelDataWorkbook, "Системные события");
    }

    /// <summary>
    /// Копия <c>samples/pdata.fwc</c>, в которой у таблицы без своего парсера (<c>VIEW_INFO</c>, один элемент) первое
    /// смещение каталога — 0x7fffffff: элемент начинается за концом таблицы. Проверка раскладки всех таблиц
    /// (<see cref="TableLayoutCheck"/>) даёт предупреждение с таблицей и номером элемента; остальное разобрано как обычно
    /// — «Тегов: 16», лист «Теги» как в эталоне.
    /// </summary>
    [Fact]
    public void BrokenUnparsedTable_Warns()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo("samples/pdata.fwc", "pdata.fwc");
        var table = FwxReader.Read(input).FindTable("VIEW_INFO")!;
        Assert.Equal(1, table.Entries);
        var bytes = File.ReadAllBytes(input);
        // каталог смещений — сразу за заголовком таблицы 0x34
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)table.Offset + FwxReader.TableHeaderSize), 0x7fffffff);
        File.WriteAllBytes(input, bytes);

        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        var warning = Assert.Single(run.Errors.Split('\n'));
        Assert.StartsWith("ПРЕДУПРЕЖДЕНИЕ: не разобрано - VIEW_INFO @ 0x", warning, StringComparison.Ordinal);
        Assert.EndsWith("отрицательная длина элемента 1", warning, StringComparison.Ordinal);
        RunAssertions.HasLine(run.Output, "Тегов: 16");
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/теги.txt");
    }

    /// <summary>
    /// Проверяет, что у каждого PLC-тега (Connection не <c>&lt;Internal tag&gt;</c>) колонки
    /// PLC name, PLC tag и Source comment — «неизвестно».
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    private static void AssertPlcColumnsUnknown(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        var wrong = workbook.Worksheet("Теги").RowsUsed().Where(r => r.RowNumber() > TagSheetDump.HeaderRow)
            .Where(r => r.Cell(3).GetString() != "<Internal tag>")
            .Where(r => r.Cell(4).GetString() != Unknown || r.Cell(5).GetString() != Unknown || r.Cell(10).GetString() != Unknown)
            .Select(r => $"строка {r.RowNumber()}: {r.Cell(1).GetString()} | {r.Cell(4).GetString()} | {r.Cell(5).GetString()} | {r.Cell(10).GetString()}")
            .ToList();
        Assert.True(wrong.Count == 0, $"у PLC-тегов колонки ПЛК не «неизвестно»:\n{string.Join("\n", wrong)}");
    }

    /// <summary>Проверяет, что в слепке листа «Теги» есть строка, равная ожидаемой целиком.</summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    /// <param name="expected">Ожидаемая строка слепка.</param>
    private static void AssertRow(string workbookPath, string expected)
    {
        var rows = TagSheetDump.Read(workbookPath);
        var name = expected.Split(" | ")[0];
        var actual = rows.FirstOrDefault(r => r.Split(" | ")[0] == name) ?? "<нет строки>";
        Assert.True(actual == expected, $"строка листа «Теги»:\n  ожидалось: {expected}\n  получено:  {actual}");
    }

    /// <summary>
    /// Слепок без одной строки листа внутреннего тега: строка удаляется, номера строк в адресах
    /// ячеек ниже неё уменьшаются на 1, в статистике «Всего тегов» и «Внутренних тегов» — на 1 меньше.
    /// </summary>
    /// <param name="lines">Строки слепка эталона.</param>
    /// <param name="row">Номер удаляемой строки листа, с единицы.</param>
    /// <returns>Ожидаемый слепок.</returns>
    private static List<string> WithoutRow(string[] lines, int row) =>
    [
        .. lines
            .Where(line => RowOf(line) != row)
            .Select(line => RowOf(line) < row ? line : CellAddress().Replace(line, m => $"{m.Groups[1].Value}{int.Parse(m.Groups[2].Value) - 1}="))
            .Select(line => StatisticsMinusOne().Replace(line, m => $"{m.Groups[1].Value}{int.Parse(m.Groups[2].Value) - 1}"))
    ];

    /// <summary>Номер строки листа по первой ячейке строки слепка (<c>A12=…</c> → 12).</summary>
    /// <returns>Номер строки.</returns>
    private static int RowOf(string line) => int.Parse(CellAddress().Match(line).Groups[2].Value);
}
