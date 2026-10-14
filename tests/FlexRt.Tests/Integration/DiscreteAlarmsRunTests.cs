using System.Buffers.Binary;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Дискретные алармы (таблицы MSG_NUMEVENT, MSG_NUMHANDLER, AREAPTR): строка консоли, строка
/// листа «Сводка» и лист «Дискретные алармы» в <c>panel_data.xlsx</c>. Раскладка таблиц и вид
/// листа сверены с TIA на A603A0097 (TIA V21, <c>samples/a603/</c>); полный эталон — CCB-A13,
/// проекта TIA которого нет.
/// </summary>
public sealed class DiscreteAlarmsRunTests
{
    /// <summary>Бэкап панели CCB-A13: 2000 дискретных алармов, один язык — русский.</summary>
    private const string CardPanel = "samples/card/pdata.fwc";

    /// <summary>Слепок листа «Дискретные алармы» по <see cref="CardPanel"/>.</summary>
    private const string Expected = "samples/expected/card/дискретные-алармы.txt";

    /// <summary>
    /// CCB-A13 с картой ПЛК CCB-A03: код 0, 2000 алармов в консоли и на «Сводке», лист совпадает
    /// с эталоном. Эталон — слепок вывода программы, сверенный с TIA только по правилам листа на
    /// A603A0097 (адреса массивов Word, классы, квитирование): тест ловит регрессии.
    /// </summary>
    [Fact]
    public void CardPanel_WithCard_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var card = dir.ExtractFromRepo("samples/card/CCB-A03.zip", "CCB-A03");
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(CardPanel), card, dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Дискретных алармов: 2000");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Дискретных алармов=2000");
        Assert.Equal(File.ReadAllLines(RepositoryPaths.InRepo(Expected)), DiscreteAlarmsSheetDump.Read(dir.PanelDataWorkbook));
    }

    /// <summary>
    /// A603A0097 (<c>samples/a603/pdata.fwc</c>, TIA V21, три языка) без данных ПЛК: код 0, 355
    /// алармов; строки эталона <c>samples/expected/a603/дискретные-алармы-сверено.txt</c> — алармы,
    /// сверенные со списком Discrete alarms в TIA после шаговых правок: Report On, классы
    /// Acknowledgement, Warnings и свой (<c>DISP</c>), квитирование HMI и PLC, абсолютный массив
    /// Word (биты 3, 12, 20, 63), поля тега и текстового списка в тексте. Адрес символьных тегов
    /// без данных ПЛК — «неизвестно».
    /// </summary>
    [Fact]
    public void A603Panel_VerifiedRows()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/a603/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Дискретных алармов: 355");
        var rows = DiscreteAlarmsSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(356, rows.Count);
        foreach (var expected in File.ReadAllLines(RepositoryPaths.InRepo("samples/expected/a603/дискретные-алармы-сверено.txt")))
        {
            Assert.Contains(expected, rows);
        }
    }

    /// <summary>
    /// Копия CCB-A13 с незнакомым видом (7) у второго элемента AREAPTR (тег квитирования HMI):
    /// элемент пропущен — код 1, предупреждение с номером и смещением элемента; таблица прочитана
    /// не целиком, поэтому у алармов без найденного тега квитирования — <c>? | ? | неизвестно</c>,
    /// а не <c>&lt;No tag&gt;</c>; тег срабатывания из первого элемента на месте.
    /// </summary>
    [Fact]
    public void SkippedAreaPtrElement_TagsUnknownNotMissing()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo(CardPanel, "pdata.fwc");
        var doc = FwxReader.Read(input);
        var item = FwxReader.Items(doc.Binary, doc.FindTable("AREAPTR")!).ElementAt(1);
        var bytes = File.ReadAllBytes(input);
        // имя «2» — один символ: заголовок 4 байта и имя с выравниванием 4 байта, затем u32 вид
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)item.Offset + 8), 7);
        File.WriteAllBytes(input, bytes);
        var run = FlexRtProcess.Run(input, dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - AREAPTR: пропущено элементов с незнакомой раскладкой: 1, первый — элемент 2 на 0x{item.Offset:x}: вид 7");
        var first = DiscreteAlarmsSheetDump.Read(dir.PanelDataWorkbook)[1];
        Assert.StartsWith("1 | ", first);
        Assert.EndsWith("| Errors | <No alarm group> | Alarms_Events | 0 | %DB1000.DBX1.0 | ? | ? | неизвестно | Alarms_Events | 2000 | Off", first);
    }

    /// <summary>
    /// Копия CCB-A13 с неверным числом классов в MSG_NUMHANDLER: длина не сходится — код 1,
    /// предупреждение с таблицей и смещением, классов нет, у всех алармов класс <c>?</c>.
    /// </summary>
    [Fact]
    public void BrokenClassTable_ClassUnknown()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo(CardPanel, "pdata.fwc");
        var doc = FwxReader.Read(input);
        var item = FwxReader.Items(doc.Binary, doc.FindTable("MSG_NUMHANDLER")!).Single();
        var bytes = File.ReadAllBytes(input);
        // число классов — u16 за числом алармов (+0x2c) и двумя массивами u16[2000]
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)item.Offset + 0x2e + 4 * 2000), 99);
        File.WriteAllBytes(input, bytes);
        var run = FlexRtProcess.Run(input, dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - MSG_NUMHANDLER @ 0x{item.Offset:X}: запись 1: длина {item.Length} не сходится с 99 классами");
        var rows = DiscreteAlarmsSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(2001, rows.Count);
        Assert.All(rows.Skip(1), r => Assert.Contains(" | ? | <No alarm group> | Alarms_Events | ", r));
    }

    /// <summary>
    /// CCB-A13 без данных ПЛК: теги алармов CCB-A13 абсолютные, адреса считаются по панели — лист
    /// тот же, что с картой.
    /// </summary>
    [Fact]
    public void CardPanel_WithoutPlc_SameSheet()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(CardPanel), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        Assert.Equal(File.ReadAllLines(RepositoryPaths.InRepo(Expected)), DiscreteAlarmsSheetDump.Read(dir.PanelDataWorkbook));
    }

    /// <summary><c>samples/pdata.fwc</c> без дискретных алармов: «Дискретных алармов: 0», листа нет.</summary>
    [Fact]
    public void SamplePanel_NoAlarms_NoSheet()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Дискретных алармов: 0");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Дискретных алармов=0");
        DiscreteAlarmsSheetDump.Absent(dir.PanelDataWorkbook);
    }

    /// <summary>
    /// Копия CCB-A13 с нечётным числом ссылок (3) у пятого элемента MSG_NUMEVENT: раскладка не
    /// сходится — код 1, предупреждение с таблицей и смещением, на листе 4 аларма, прочитанные до
    /// сбоя.
    /// </summary>
    [Fact]
    public void BrokenEventRecord_WarnsAndKeepsEarlierAlarms()
    {
        using var dir = new TestDirectory();
        var input = dir.CopyFromRepo(CardPanel, "pdata.fwc");
        var doc = FwxReader.Read(input);
        var item = FwxReader.Items(doc.Binary, doc.FindTable("MSG_NUMEVENT")!).ElementAt(4);
        var bytes = File.ReadAllBytes(input);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)item.Offset + 4), 3);
        File.WriteAllBytes(input, bytes);
        var run = FlexRtProcess.Run(input, dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - MSG_NUMEVENT @ 0x{item.Offset:X}: запись 5: длина {item.Length} не сходится с 3 ссылками");
        RunAssertions.HasLine(run.StdOut, "Дискретных алармов: 4");
        Assert.Equal(5, DiscreteAlarmsSheetDump.Read(dir.PanelDataWorkbook).Count);
        RunAssertions.NoStackTrace(run);
    }
}
