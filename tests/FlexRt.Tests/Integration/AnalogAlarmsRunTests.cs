using System.Buffers.Binary;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Аналоговые алармы (таблицы MSG_ANALOGCREAT, MSG_ANALOG_OBSE, MSG_ANALOGHANDL): строка консоли,
/// строка листа «Сводка» и лист «Аналоговые алармы» в <c>panel_data.xlsx</c>. Образец — шаговая
/// копия A603A0097 (TIA V21) с четырьмя аналоговыми алармами, сверенными со списком Analog
/// alarms в TIA.
/// </summary>
public sealed class AnalogAlarmsRunTests
{
    /// <summary>A603A0097 с аналоговыми алармами 1, 2, 3 и 7: предел-константа Int, Real, UInt и предел-тег.</summary>
    private const string AnalogPanel = "samples/a603-analog/pdata.fwc";

    /// <summary>Лист «Аналоговые алармы» по <see cref="AnalogPanel"/>, сверенный с TIA целиком.</summary>
    private const string Expected = "samples/expected/a603-analog/аналоговые-алармы.txt";

    /// <summary>
    /// A603A0097 без данных ПЛК: код 0, 4 аларма в консоли и на «Сводке», лист совпадает с
    /// эталоном: ID, текст с полем тега, классы Errors и Warnings, теги срабатывания, пределы
    /// <c>50</c>, <c>13</c>, <c>555</c> и тег предела, Limit mode, Report.
    /// </summary>
    [Fact]
    public void AnalogPanel_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(AnalogPanel), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Аналоговых алармов: 4");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Аналоговых алармов=4");
        Assert.Equal(File.ReadAllLines(RepositoryPaths.InRepo(Expected)), AnalogAlarmsSheetDump.Read(dir.PanelDataWorkbook));
    }

    /// <summary><c>samples/pdata.fwc</c> без аналоговых алармов: «Аналоговых алармов: 0», листа нет.</summary>
    [Fact]
    public void SamplePanel_NoAlarms_NoSheet()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Аналоговых алармов: 0");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Аналоговых алармов=0");
        AnalogAlarmsSheetDump.Absent(dir.PanelDataWorkbook);
    }

    /// <summary>
    /// Копия с числом ссылок 5 у второго элемента MSG_ANALOG_OBSE: длина не сходится — код 1,
    /// предупреждение с таблицей и смещением; предел аларма 7 прочитан до сбоя и выведен, у
    /// остальных Trigger tag, Limit и Limit mode — <c>?</c>.
    /// </summary>
    [Fact]
    public void BrokenLimitRecord_WarnsAndKeepsEarlierLimits()
    {
        using var dir = new TestDirectory();
        var item = Corrupt(dir, "MSG_ANALOG_OBSE", 1, 4, 5);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - MSG_ANALOG_OBSE @ 0x{item.Offset:X}: запись 2: длина {item.Length} не сходится с 5 ссылками");
        var rows = AnalogAlarmsSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(5, rows.Count);
        Assert.EndsWith("| Warnings | <No alarm group> | DB_ST7_A603_SQL_COMM_TRACE_O_FB_Active_State | DB_ST7_Cylinders_BLOCKADES_Y5_BLOCK_NR | Higher | On", rows[4]);
        Assert.All(rows.Skip(1).Take(3), r => Assert.EndsWith("| Errors | <No alarm group> | ? | ? | ? | On", r));
        RunAssertions.NoStackTrace(run);
    }

    /// <summary>
    /// Копия с числом ссылок 1 (нечётно) у третьего элемента MSG_ANALOGCREAT: раскладка не
    /// сходится — код 1, предупреждение с таблицей и смещением, на листе 2 аларма, прочитанные до
    /// сбоя.
    /// </summary>
    [Fact]
    public void BrokenAlarmRecord_WarnsAndKeepsEarlierAlarms()
    {
        using var dir = new TestDirectory();
        var item = Corrupt(dir, "MSG_ANALOGCREAT", 2, 4, 1);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - MSG_ANALOGCREAT @ 0x{item.Offset:X}: запись 3: длина {item.Length} не сходится с 1 ссылками");
        RunAssertions.HasLine(run.StdOut, "Аналоговых алармов: 2");
        Assert.Equal(3, AnalogAlarmsSheetDump.Read(dir.PanelDataWorkbook).Count);
        RunAssertions.NoStackTrace(run);
    }

    /// <summary>
    /// Копия с неверным числом классов в MSG_ANALOGHANDL: длина не сходится — код 1,
    /// предупреждение с таблицей и смещением, классов нет, у всех алармов класс <c>?</c>.
    /// </summary>
    [Fact]
    public void BrokenClassTable_ClassUnknown()
    {
        using var dir = new TestDirectory();
        // число классов — u16 за числом алармов (+0x2c) и двумя массивами u16[4]
        var item = Corrupt(dir, "MSG_ANALOGHANDL", 0, 0x2e + 4 * 4, 99, 2);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - MSG_ANALOGHANDL @ 0x{item.Offset:X}: запись 1: длина {item.Length} не сходится с 99 классами");
        var rows = AnalogAlarmsSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(5, rows.Count);
        Assert.All(rows.Skip(1), r => Assert.Contains(" | ? | <No alarm group> | DB_ST7_", r));
    }

    /// <summary>
    /// Скопировать <see cref="AnalogPanel"/> в каталог теста как <c>pdata.fwc</c> и записать число
    /// <paramref name="value"/> (<paramref name="size"/> байт, little endian) в элемент
    /// <paramref name="index"/> таблицы <paramref name="table"/> со смещением <paramref name="offset"/>.
    /// </summary>
    /// <returns>Изменённый элемент: смещение и длина до правки.</returns>
    private static TableItem Corrupt(TestDirectory dir, string table, int index, int offset, uint value, int size = 4)
    {
        var input = dir.CopyFromRepo(AnalogPanel, "pdata.fwc");
        var doc = FwxReader.Read(input);
        var item = FwxReader.Items(doc.Binary, doc.FindTable(table)!).ElementAt(index);
        var bytes = File.ReadAllBytes(input);
        var span = bytes.AsSpan((int)item.Offset + offset);
        if (size == 2)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        }
        File.WriteAllBytes(input, bytes);
        return item;
    }
}
