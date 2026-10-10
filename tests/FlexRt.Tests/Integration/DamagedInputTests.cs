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
/// Ожидания — раздел «Установленное поведение» в <c>CLAUDE.md</c>.
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
    /// <c>samples/plc/pdata.fwc</c> (два соединения) без файла ПЛК: код 0, строка «не найден», у
    /// PLC-тегов колонки ПЛК «неизвестно», тип — по коду панели (<c>USInt/Char</c>, <c>DInt/Time</c>);
    /// 645 строк языков и hex-файлы совпадают с эталоном — без данных ПЛК такая панель читается.
    /// </summary>
    [Fact]
    public void PlcPanel_WithoutPlcFile_PlcColumnsUnknown()
    {
        using var dir = new TestDirectory();
        var plc = dir.MissingFile();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/plc/pdata.fwc"), plc, dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Тегов: 69");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {plc} не найден - данные ПЛК: неизвестно");
        AssertPlcColumnsUnknown(dir.PanelDataWorkbook);
        AssertRow(dir.PanelDataWorkbook, CharIb3Row);
        AssertRow(dir.PanelDataWorkbook, DIntId6Row);
        AssertRow(dir.PanelDataWorkbook, "A4=Найдено в ПЛК | B4=0");
        AssertRow(dir.PanelDataWorkbook, "A6=Данные ПЛК неизвестны | B6=50");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, $"Данные ПЛК=не найден: {plc}");
        RunAssertions.StringsSheet(dir.StringsWorkbook, 645);
        RunAssertions.HexMatches(dir.HexDir, "samples/expected/plc/pdata.sha256");
    }

    /// <summary>
    /// Копия <c>samples/plc/PEData.plf</c> с инвертированным байтом в середине данных первого
    /// кадра, панель <c>samples/card/pdata.fwc</c> (одно соединение): SHA-256 кадра не совпадает.
    /// Код 1, предупреждение в stderr, лист «Ошибки» с сообщением, колонки ПЛК «неизвестно».
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

        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/card/pdata.fwc"), plc, dir.OutDir);

        var message = $"{plc}: PEData.plf @ 0x{PlfHeaderSize:X}: контрольная сумма кадра не совпала";
        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {message}");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {plc} не прочитан - данные ПЛК: неизвестно");
        RunAssertions.NoStackTrace(run);
        RunAssertions.ErrorsSheetContains(dir.PanelDataWorkbook, message);
        SummarySheetDump.Contains(dir.PanelDataWorkbook, $"Данные ПЛК=не прочитан: {plc}");
        AssertPlcColumnsUnknown(dir.PanelDataWorkbook);
    }

    /// <summary>
    /// Копия <c>samples/pdata.fwc</c>, в которой у записи VAR из середины таблицы первое
    /// слово 4 вместо 3. Смещение записи — из разбора той же копии через
    /// <see cref="FwxReader"/> (TOC → каталог смещений VAR). Код 1, сводное предупреждение
    /// VAR с номером записи и смещением, тегов на один меньше, остальные строки листа «Теги»
    /// — как в эталоне.
    /// </summary>
    [Fact]
    public void BrokenVarRecord_SkippedWithWarning()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo("samples/pdata.fwc", "pdata.fwc");
        var tags = FwxReader.Read(input).Tags;
        var broken = tags[tags.Count / 2];
        var bytes = File.ReadAllBytes(input);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)broken.Offset), 4);
        File.WriteAllBytes(input, bytes);

        var run = FlexRtProcess.Run(input, dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdOut, $"Тегов: {tags.Count - 1}");
        RunAssertions.HasLine(run.StdErr,
            $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - VAR: пропущено записей с незнакомой раскладкой: 1, первая — VAR @ 0x{broken.Offset:X}: запись {broken.Index}: первое слово 0x4, ожидалось 0x3");
        RunAssertions.NoStackTrace(run);
        var expected = WithoutRow(File.ReadAllLines(RepositoryPaths.InRepo("samples/expected/теги.txt")), TagSheetDump.HeaderRow + broken.Index);
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, expected, $"samples/expected/теги.txt без строки {TagSheetDump.HeaderRow + broken.Index} ({broken.Name}), всего и внутренних на 1 меньше");
    }

    /// <summary>
    /// Копия <c>samples/pdata.fwc</c>, в которой число событий SYSMSGHANDLER (+0x2a) — 0xffff:
    /// список не помещается в запись. Код 1, предупреждение с таблицей и смещением записи,
    /// «Системных событий: 0», листа «Системные события» нет, лист «Теги» — как в эталоне.
    /// </summary>
    [Fact]
    public void BrokenSysMsgHandler_WarnsAndNoEventsSheet()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo("samples/pdata.fwc", "pdata.fwc");
        var doc = FwxReader.Read(input);
        var item = FwxReader.Items(doc.Binary, doc.FindTable("SYSMSGHANDLER")!).Single();
        var bytes = File.ReadAllBytes(input);
        // +0x2a — число событий, см. SysMsgHandlerParser
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)item.Offset + 0x2a), 0xffff);
        File.WriteAllBytes(input, bytes);

        var run = FlexRtProcess.Run(input, dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdOut, "Системных событий: 0");
        RunAssertions.HasLine(run.StdErr,
            $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SYSMSGHANDLER @ 0x{item.Offset:X}: запись 1: 65535 событий не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        SystemEventsSheetDump.Absent(dir.PanelDataWorkbook);
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
