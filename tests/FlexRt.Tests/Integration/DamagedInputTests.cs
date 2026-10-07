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
        "A30=Char_IB3 | B30=USInt/Char | C30=HMI_Connection_1 | D30=неизвестно | E30=неизвестно | G30=<symbolic access> | H30=1 s | J30=неизвестно";

    /// <summary>
    /// Строка <c>DInt_ID6</c> (абсолютный доступ, код ПЛК 0x04) без данных ПЛК: тип
    /// <c>DInt/Time</c>, адрес из панели <c>%ID6</c>, колонки ПЛК «неизвестно».
    /// </summary>
    private const string DIntId6Row =
        "A35=DInt_ID6 | B35=DInt/Time | C35=HMI_Connection_1 | D35=неизвестно | E35=неизвестно | F35=%ID6 | G35=<absolute access> | H35=1 s | J35=неизвестно";

    /// <summary>Адрес ячейки в начале ячейки слепка: <c>A12=</c>.</summary>
    [GeneratedRegex(@"(?<=^| \| )([A-Z]+)(\d+)=")]
    private static partial Regex CellAddress();

    /// <summary>
    /// <c>samples/plc/pdata.fwc</c> без файла ПЛК: код 0, строка «не найден», у PLC-тегов
    /// колонки ПЛК «неизвестно», тип — по коду панели (<c>USInt/Char</c>, <c>DInt/Time</c>).
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
    }

    /// <summary>
    /// Копия <c>samples/plc/PEData.plf</c> с инвертированным байтом в середине данных первого
    /// кадра: SHA-256 кадра не совпадает. Код 1, предупреждение в stderr, лист «Ошибки» с
    /// сообщением, колонки ПЛК «неизвестно».
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

        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/plc/pdata.fwc"), plc, dir.OutDir);

        var message = $"{plc}: PEData.plf @ 0x{PlfHeaderSize:X}: контрольная сумма кадра не совпала";
        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {message}");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {plc} не прочитан - данные ПЛК: неизвестно");
        RunAssertions.NoStackTrace(run);
        RunAssertions.ErrorsSheetContains(dir.PanelDataWorkbook, message);
        AssertPlcColumnsUnknown(dir.PanelDataWorkbook);
        AssertRow(dir.PanelDataWorkbook, CharIb3Row);
        AssertRow(dir.PanelDataWorkbook, DIntId6Row);
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
        var expected = WithoutRow(File.ReadAllLines(RepositoryPaths.InRepo("samples/expected/теги.txt")), broken.Index + 1);
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, expected, $"samples/expected/теги.txt без строки {broken.Index + 1} ({broken.Name})");
    }

    /// <summary>
    /// Проверяет, что у каждого PLC-тега (Connection не <c>&lt;Internal tag&gt;</c>) колонки
    /// PLC name, PLC tag и Source comment — «неизвестно».
    /// </summary>
    /// <param name="workbookPath">Путь к <c>panel_data.xlsx</c>.</param>
    private static void AssertPlcColumnsUnknown(string workbookPath)
    {
        using var workbook = new XLWorkbook(workbookPath);
        var wrong = workbook.Worksheet("Теги").RowsUsed().Skip(1)
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
    /// Слепок без одной строки листа: строка удаляется, номера строк в адресах ячеек ниже
    /// неё уменьшаются на 1.
    /// </summary>
    /// <param name="lines">Строки слепка эталона.</param>
    /// <param name="row">Номер удаляемой строки листа, с единицы.</param>
    /// <returns>Ожидаемый слепок.</returns>
    private static List<string> WithoutRow(string[] lines, int row) =>
    [
        .. lines
            .Where((_, i) => i + 1 != row)
            .Select((line, i) => i + 1 < row ? line : CellAddress().Replace(line, m => $"{m.Groups[1].Value}{i + 1}="))
    ];
}
