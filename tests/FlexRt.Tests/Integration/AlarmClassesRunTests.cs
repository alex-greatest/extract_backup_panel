using System.Buffers.Binary;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Классы алармов (MSG_NUMHANDLER, RECMSG, ARCHIVES, MSG_LOGGER, FUNC_SEND_EMAIL): строка консоли,
/// строка листа «Сводка» и лист «Классы алармов» в <c>panel_data.xlsx</c> с заливкой цветов фона.
/// Образец — шаговая копия A603A0097 (TIA V21, TP1900 Comfort, 3 языка) после правок классов,
/// сверенных со списком Alarm classes в TIA.
/// </summary>
public sealed class AlarmClassesRunTests
{
    /// <summary>A603A0097 с шестью классами: журнал у 1 и 2, адрес письма и свои цвета у 64.</summary>
    private const string ClassesPanel = "samples/a603-classes/pdata.fwc";

    /// <summary>
    /// Project1 (V21) с цветами классов, двумя журналами алармов и двумя адресами: у класса 65 —
    /// <c>Alarm_log_1</c> и <c>a@test.com</c>, у 66 — <c>Alarm_log_2</c> и <c>b@test.com</c>; сверено с TIA.
    /// </summary>
    private const string TwoLogsPanel = "samples/project1-classes/pdata.fwc";

    /// <summary>Лист «Классы алармов» по <see cref="ClassesPanel"/> целиком.</summary>
    private const string Expected = "samples/expected/a603-classes/классы-алармов.txt";

    /// <summary>Колонка журнала класса.</summary>
    private const string LogColumn = "Log";

    /// <summary>Колонка адреса письма класса.</summary>
    private const string EmailColumn = "E-mail address";

    /// <summary>Текст ячейки цвета, когда цвета классов выключены в панели.</summary>
    private const string ColorsOff = "цвета классов выключены";

    /// <summary>Колонка Log <see cref="ClassesPanel"/> по классам 1, 2, 33, 34, 64, 65.</summary>
    private static readonly string[] Logs = ["OP70_AlarmsHistory", "OP70_AlarmsHistory", "<No log>", "<No log>", "<No log>", "<No log>"];

    /// <summary>Колонка E-mail address <see cref="ClassesPanel"/> по классам 1, 2, 33, 34, 64, 65.</summary>
    private static readonly string[] Emails = ["", "", "", "", "test@test.com", ""];

    /// <summary>Колонка, прочитанная не целиком: <c>?</c> у каждого из шести классов.</summary>
    private static readonly string[] AllUnknown = ["?", "?", "?", "?", "?", "?"];

    /// <summary>
    /// A603A0097 без данных ПЛК: код 0, 6 классов в консоли и на «Сводке», лист совпадает с
    /// эталоном: Display name и тексты состояний на трёх языках, имена стандартных классов, State
    /// machine, журнал <c>OP70_AlarmsHistory</c> у классов 1 и 2, адрес <c>test@test.com</c> у 64,
    /// цвета фона.
    /// </summary>
    [Fact]
    public void ClassesPanel_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(ClassesPanel), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Классов алармов: 6");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Классов алармов=6");
        Assert.Equal(File.ReadAllLines(RepositoryPaths.InRepo(Expected)), AlarmClassesSheetDump.Read(dir.PanelDataWorkbook));
    }

    /// <summary>
    /// Заливка колонок цвета фона по <see cref="ClassesPanel"/>: цвет ячейки равен её тексту; на
    /// тёмной заливке (<c>0, 0, 255</c> и <c>0, 0, 0</c> у класса 64) текст белый, на красной,
    /// жёлтой, зелёной <c>51, 153, 102</c> и белой — нет.
    /// </summary>
    [Fact]
    public void ClassesPanel_BackgroundFills()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(ClassesPanel), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        string[] expected =
        [
            "1 | FF0000 | FFCC00 | FFFFFF | FFFFFF",
            "2 | FFFFFF | FFFFFF | FFFFFF | FFFFFF",
            "33 | FF0000 | FF0000 | FFFFFF | FFFFFF",
            "34 | FF0000 | FF0000 | FFFFFF | FFFFFF",
            "64 | FF0000 | 0000FF белый | 339966 | 000000 белый",
            "65 | FF0000 | FF0000 | FFFFFF | FFFFFF"
        ];
        Assert.Equal(expected, AlarmClassesSheetDump.Fills(dir.PanelDataWorkbook));
    }

    /// <summary>
    /// <see cref="TwoLogsPanel"/>: код 0, без предупреждений, у каждого класса свой журнал и свой
    /// адрес — записи MSG_LOGGER по 6 байт, номер элемента FUNC_SEND_EMAIL 0 и 1.
    /// </summary>
    [Fact]
    public void TwoLogsPanel_LogAndEmailPerClass()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(TwoLogsPanel), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        Assert.DoesNotContain("ПРЕДУПРЕЖДЕНИЕ", run.StdErr + run.StdOut);
        var rows = AlarmClassesSheetDump.Read(dir.PanelDataWorkbook);
        string[] logs = ["<No log>", "<No log>", "<No log>", "<No log>", "<No log>", "Alarm_log_1", "Alarm_log_2"];
        string[] emails = ["", "", "", "", "", "a@test.com", "b@test.com"];
        Assert.Equal(logs, AlarmClassesSheetDump.Column(rows, LogColumn));
        Assert.Equal(emails, AlarmClassesSheetDump.Column(rows, EmailColumn));
    }

    /// <summary>
    /// <c>samples/pdata.fwc</c> (Project1): цвета классов выключены в панели — во всех четырёх
    /// колонках цвета текст «цвета классов выключены» без заливки; журнала и адреса нет.
    /// </summary>
    [Fact]
    public void SamplePanel_ClassColorsOff()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Классов алармов: 2");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Классов алармов=2");
        var colors = string.Join(" | ", Enumerable.Repeat(ColorsOff, 4));
        string[] expected =
        [
            "ID | Display name 0x409 — English (United States) | Name | State machine | Log | E-mail address | Background color \"Incoming\" | "
                + "Background color \"Incoming/Outgoing\" | Background color \"Incoming/Acknowledged\" | Background color \"Incoming/Outgoing/Acknowledged\" | "
                + "Incoming 0x409 — English (United States) | Outgoing 0x409 — English (United States) | Acknowledged 0x409 — English (United States)",
            $"1 | ! | Errors | Alarm with single-mode acknowledgment | <No log> |  | {colors} | I | O | A",
            $"2 |  | Warnings | Alarm without acknowledgment | <No log> |  | {colors} | I | O | A"
        ];
        Assert.Equal(expected, AlarmClassesSheetDump.Read(dir.PanelDataWorkbook));
        Assert.Equal(["1 | нет | нет | нет | нет", "2 | нет | нет | нет | нет"], AlarmClassesSheetDump.Fills(dir.PanelDataWorkbook));
    }

    /// <summary>
    /// Копия с числом классов 99 в элементе RECMSG: длина не сходится — код 1, предупреждение с
    /// таблицей и смещением; лист записан, Log у всех классов <c>?</c>, адрес письма не затронут.
    /// </summary>
    [Fact]
    public void BrokenRecMsg_LogUnknown()
    {
        using var dir = new TestDirectory();
        // число классов K — u16 за u32 1, u32 числом ссылок 1, ссылкой 8 байт и u16 2
        var item = Corrupt(dir, "RECMSG", 0, 0x12, 99, 2);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        AssertWarned(run, $"RECMSG @ 0x{item.Offset:X}: запись 1: длина {item.Length} не сходится с 99 классами");
        var rows = AlarmClassesSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(7, rows.Count);
        Assert.Equal(AllUnknown, AlarmClassesSheetDump.Column(rows, LogColumn));
        Assert.Equal(Emails, AlarmClassesSheetDump.Column(rows, EmailColumn));
    }

    /// <summary>
    /// Копия с первым словом 4 у третьего элемента ARCHIVES (журнал алармов
    /// <c>OP70_AlarmsHistory</c>): код 1, предупреждение с таблицей и смещением; лист записан, Log
    /// у всех классов <c>?</c>, адрес письма не затронут.
    /// </summary>
    [Fact]
    public void BrokenArchives_LogUnknown()
    {
        using var dir = new TestDirectory();
        var item = Corrupt(dir, "ARCHIVES", 2, 0, 4, 2);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        AssertWarned(run, $"ARCHIVES @ 0x{item.Offset:X}: запись 3: первое слово не 3");
        var rows = AlarmClassesSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(7, rows.Count);
        Assert.Equal(AllUnknown, AlarmClassesSheetDump.Column(rows, LogColumn));
        Assert.Equal(Emails, AlarmClassesSheetDump.Column(rows, EmailColumn));
    }

    /// <summary>
    /// Копия с числом записей 99 в элементе MSG_LOGGER: длина не сходится — код 1, предупреждение с
    /// таблицей и смещением; лист записан, E-mail address у всех классов <c>?</c>, журнал не
    /// затронут.
    /// </summary>
    [Fact]
    public void BrokenMsgLogger_EmailUnknown()
    {
        using var dir = new TestDirectory();
        // число записей K — u16 за u32 1, u32 числом ссылок 1, ссылкой 8 байт и u16 1
        var item = Corrupt(dir, "MSG_LOGGER", 0, 0x12, 99, 2);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        AssertWarned(run, $"MSG_LOGGER @ 0x{item.Offset:X}: запись 1: длина {item.Length} не сходится с 99 записями");
        var rows = AlarmClassesSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(7, rows.Count);
        Assert.Equal(Logs, AlarmClassesSheetDump.Column(rows, LogColumn));
        Assert.Equal(AllUnknown, AlarmClassesSheetDump.Column(rows, EmailColumn));
    }

    /// <summary>
    /// Копия с нечётной длиной адреса (3 байта) в элементе FUNC_SEND_EMAIL: код 1, предупреждение с
    /// таблицей и смещением; лист записан, E-mail address у всех классов <c>?</c>, журнал не затронут.
    /// </summary>
    [Fact]
    public void BrokenFuncSendEmail_EmailUnknown()
    {
        using var dir = new TestDirectory();
        // +0x50 — u32 длина адреса в байтах, см. FuncSendEmailParser
        var item = Corrupt(dir, "FUNC_SEND_EMAIL", 0, 0x50, 3);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        AssertWarned(run, $"FUNC_SEND_EMAIL @ 0x{item.Offset:X}: запись 1: адрес из 3 байт не помещается в {item.Length} байт");
        var rows = AlarmClassesSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(7, rows.Count);
        Assert.Equal(Logs, AlarmClassesSheetDump.Column(rows, LogColumn));
        Assert.Equal(AllUnknown, AlarmClassesSheetDump.Column(rows, EmailColumn));
    }

    /// <summary>
    /// Запуск на испорченной копии: код 1, в stderr предупреждение «не разобрано» с
    /// <paramref name="message"/>, без трассы стека, классов по-прежнему 6.
    /// </summary>
    /// <param name="run">Результат запуска.</param>
    /// <param name="message">Таблица, смещение и текст ошибки формата.</param>
    private static void AssertWarned(ProgramRun run, string message)
    {
        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - {message}");
        RunAssertions.HasLine(run.StdOut, "Классов алармов: 6");
        RunAssertions.NoStackTrace(run);
    }

    /// <summary>
    /// Скопировать <see cref="ClassesPanel"/> в каталог теста как <c>pdata.fwc</c> и записать число
    /// <paramref name="value"/> (<paramref name="size"/> байт, little endian) в элемент
    /// <paramref name="index"/> таблицы <paramref name="table"/> со смещением <paramref name="offset"/>.
    /// </summary>
    /// <returns>Изменённый элемент: смещение и длина до правки.</returns>
    private static TableItem Corrupt(TestDirectory dir, string table, int index, int offset, uint value, int size = 4)
    {
        var input = dir.CopyFromRepo(ClassesPanel, "pdata.fwc");
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
