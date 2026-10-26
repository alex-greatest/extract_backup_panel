using System.Buffers.Binary;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Экраны панели (SCREEN, номера из ADAPTVALUECHANG, стартовый экран из GUI): лист «Экраны», строки
/// «Экранов» и «Стартовый экран» в тексте запуска и на «Сводке». Испорченные входы — копии <c>samples/pdata.fwc</c>,
/// изменённые в рантайме во временном каталоге теста.
/// </summary>
public sealed class ScreensRunTests
{
    /// <summary>Заголовок листа «Экраны» панели с одним языком 0x409.</summary>
    internal const string Header = "Name | Number | Start screen | Width | Height | Background color | Visibility tag | Visibility from | Visibility to | Visibility | Tooltip 0x409 — English (United States) | Вид";

    /// <summary>
    /// <c>samples/pdata.fwc</c>: Permanent area (запись <c>Overview</c>), <c>Root screen</c> — номер 1 и стартовый,
    /// четыре slide-in TP700 Comfort. «Экранов: 1», «Стартовый экран: Root screen (1)» в тексте запуска и на «Сводке», без предупреждений.
    /// </summary>
    [Fact]
    public void SamplePanel_ScreensSheetAndStartScreen()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), null, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Экранов: 1");
        RunAssertions.HasLine(run.Output, "Стартовый экран: Root screen (1)");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Экранов=1", "Стартовый экран=Root screen (1)");
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 60 | 182, 182, 182 |  |  |  |  |  | Permanent area",
            "Root screen | 1 | Yes | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen",
            "Slide-in screen left |  |  | 267 | 480 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen bottom |  |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen right |  |  | 267 | 480 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen top |  |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen"
        ], SheetDump.Read(dir.PanelDataWorkbook, "Экраны"));
    }

    /// <summary>
    /// <c>samples/screens/05_popup.fwc</c> — шаговая копия Project1 (TIA V21, TP700 Comfort), сверенная с TIA:
    /// экраны по возрастанию номера, стартовый — <c>Screen_2</c> с номером 5, pop-up <c>Pop-up screen_1</c>
    /// 400×200 без номера; служебная запись pop-up на лист не попадает.
    /// </summary>
    [Fact]
    public void PopupPanel_NumbersStartScreenAndPopup()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/screens/05_popup.fwc"), null, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Экранов: 3");
        RunAssertions.HasLine(run.Output, "Стартовый экран: Screen_2 (5)");
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 60 | 182, 182, 182 |  |  |  |  |  | Permanent area",
            "Root screen | 1 |  | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen",
            "Screen_1 | 2 |  | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen",
            "Screen_2 | 5 | Yes | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen",
            "Pop-up screen_1 |  |  | 400 | 200 | 182, 182, 182 |  |  |  |  |  | Pop-up screen",
            "Slide-in screen top |  |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen left |  |  | 267 | 480 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen bottom |  |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen right |  |  | 267 | 480 | 182, 182, 182 |  |  |  |  |  | Slide-in screen"
        ], SheetDump.Read(dir.PanelDataWorkbook, "Экраны"));
    }

    /// <summary>
    /// A603A0097 (TIA V21, TP1900 Comfort): 49 экранов с номерами, стартовый <c>998_LOGIN</c> (998), один pop-up и
    /// четыре slide-in; три языка — три колонки подсказки, подсказок нет. Цвет фона <c>998_LOGIN</c> — 182, 182, 182,
    /// остальных экранов — белый (как BackColor в XML TIA).
    /// </summary>
    [Fact]
    public void A603_StartScreenByNumber()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/a603/pdata.fwc"), null, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Экранов: 49");
        RunAssertions.HasLine(run.Output, "Стартовый экран: 998_LOGIN (998)");
        var rows = SheetDump.Read(dir.PanelDataWorkbook, "Экраны");
        Assert.Equal(1 + 1 + 49 + 1 + 4, rows.Count);
        Assert.Equal("Permanent area |  |  | 1366 | 0 | 182, 182, 182 |  |  |  |  |  |  |  | Permanent area", rows[1]);
        Assert.Equal("001_AUTO_SCREEN | 1 |  | 1366 | 768 | 255, 255, 255 |  |  |  |  |  |  |  | Screen", rows[2]);
        Assert.Equal("998_LOGIN | 998 | Yes | 1366 | 768 | 182, 182, 182 |  |  |  |  |  |  |  | Screen", rows[49]);
        Assert.Equal("No_Authorization_Msg_PopUpScreen |  |  | 800 | 60 | 182, 182, 182 |  |  |  |  |  |  |  | Pop-up screen", rows[51]);
        Assert.Single(rows, r => r.Contains(" | Yes | "));
    }

    /// <summary>
    /// Бэкап CCB-A13 (Runtime V17, TP1500 Comfort): 10 экранов с номерами, стартовый <c>001_MainScreen</c> (1),
    /// семь pop-up 636×322 (<c>UZ70</c>…<c>UZ90</c>) — на каждый в SCREEN своя служебная запись; цвет фона pop-up —
    /// 181, 182, 181.
    /// </summary>
    [Fact]
    public void Card_SevenPopups()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/card/pdata.fwc"), null, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Экранов: 10");
        RunAssertions.HasLine(run.Output, "Стартовый экран: 001_MainScreen (1)");
        var rows = SheetDump.Read(dir.PanelDataWorkbook, "Экраны");
        Assert.Equal("Permanent area |  |  | 1280 | 0 | 182, 182, 182 |  |  |  |  |  | Permanent area", rows[1]);
        Assert.Equal("001_MainScreen | 1 | Yes | 1280 | 800 | 182, 182, 182 |  |  |  |  |  | Screen", rows[2]);
        Assert.Equal(["UZ70", "UZ60", "UZ40", "HPU", "UZ10", "UZ72", "UZ90"],
            rows.Where(r => r.EndsWith(" | 636 | 322 | 181, 182, 181 |  |  |  |  |  | Pop-up screen")).Select(r => r.Split(" | ")[0]));
    }

    /// <summary>
    /// Первое слово третьей записи SCREEN — 7: предупреждение с таблицей и смещением, две записи до сбоя
    /// (Permanent area и <c>Root screen</c>) остаются на листе.
    /// </summary>
    [Fact]
    public void BrokenScreenRecord_WarnsAndKeepsScreensBefore()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "SCREEN", 2, 0, 7);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SCREEN @ 0x{item.Offset:X}: запись 3: первое слово 7, ожидалось 3 или 1");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Стартовый экран: Root screen (1)");
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 60 | 182, 182, 182 |  |  |  |  |  | Permanent area",
            "Root screen | 1 | Yes | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen"
        ], SheetDump.Read(dir.PanelDataWorkbook, "Экраны"));
    }

    /// <summary>
    /// Число объектов третьей записи SCREEN (<c>Slide-in screen left</c>, объектов нет, хвост 20 байт) — 3: объекты
    /// помещаются, но после них 8 байт — цвет фона не читается: одно сводное предупреждение с записью и смещением, все
    /// записи остаются на листе, у этой Background color — <c>?</c>.
    /// </summary>
    [Fact]
    public void ShortScreenTail_WarnsAndKeepsAllScreens()
    {
        using var dir = new TestDirectory();
        // имя из 20 символов, ссылок нет: число объектов на +0x42
        var (input, item) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "SCREEN", 2, 0x42, 3);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SCREEN: цвет фона не прочитан у записей: 1 (после объектов меньше 12 байт), первая — запись 3 @ 0x{item.Offset:X}");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Экранов: 1");
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 60 | 182, 182, 182 |  |  |  |  |  | Permanent area",
            "Root screen | 1 | Yes | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen",
            "Slide-in screen left |  |  | 267 | 480 | ? |  |  |  |  |  | Slide-in screen",
            "Slide-in screen bottom |  |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen right |  |  | 267 | 480 | 182, 182, 182 |  |  |  |  |  | Slide-in screen",
            "Slide-in screen top |  |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen"
        ], SheetDump.Read(dir.PanelDataWorkbook, "Экраны"));
    }

    /// <summary>
    /// Короткий хвост третьей записи SCREEN и первое слово 7 у пятой: разбор обрывается на пятой, но сводное предупреждение
    /// о цвете не теряется и идёт перед предупреждением о сбое; четыре записи до сбоя на листе, у третьей цвет <c>?</c>.
    /// </summary>
    [Fact]
    public void ShortTailThenBrokenRecord_KeepsColorWarning()
    {
        using var dir = new TestDirectory();
        var (input, shortTail) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "SCREEN", 2, 0x42, 3);
        var broken = WriteU16(input, "SCREEN", 4, 0, 7);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        var color = $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SCREEN: цвет фона не прочитан у записей: 1 (после объектов меньше 12 байт), первая — запись 3 @ 0x{shortTail.Offset:X}";
        var failure = $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SCREEN @ 0x{broken.Offset:X}: запись 5: первое слово 7, ожидалось 3 или 1";
        RunAssertions.HasLine(run.Errors, color);
        RunAssertions.HasLine(run.Errors, failure);
        Assert.True(run.Errors.IndexOf(color, StringComparison.Ordinal) < run.Errors.IndexOf(failure, StringComparison.Ordinal), "предупреждение о цвете — после сбоя");
        RunAssertions.NoStackTrace(run);
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 60 | 182, 182, 182 |  |  |  |  |  | Permanent area",
            "Root screen | 1 | Yes | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen",
            "Slide-in screen left |  |  | 267 | 480 | ? |  |  |  |  |  | Slide-in screen",
            "Slide-in screen bottom |  |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen"
        ], SheetDump.Read(dir.PanelDataWorkbook, "Экраны"));
    }

    /// <summary>
    /// Число пар элемента ADAPTVALUECHANG со ссылкой на GUI — 0xffff: предупреждение; номера не известны —
    /// Number и вид <c>Root screen</c> <c>?</c>, «Экранов: 0», стартовый экран <c>неизвестно (1)</c>.
    /// </summary>
    [Fact]
    public void BrokenAdaptValueChang_NumbersUnknown()
    {
        using var dir = new TestDirectory();
        // одна ссылка (на GUI): ссылки кончаются на +0x10, за ними u16 и u16 число пар
        var (input, item) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "ADAPTVALUECHANG", 0, 0x12, 0xffff);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - ADAPTVALUECHANG @ 0x{item.Offset:X}: запись 1: 65535 пар не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Экранов: 0");
        RunAssertions.HasLine(run.Output, "Стартовый экран: неизвестно (1)");
        var rows = SheetDump.Read(dir.PanelDataWorkbook, "Экраны");
        Assert.Contains("Root screen | ? |  | 800 | 420 | 182, 182, 182 |  |  |  |  |  | ?", rows);
        Assert.Contains("Slide-in screen top | ? |  | 800 | 160 | 182, 182, 182 |  |  |  |  |  | Slide-in screen", rows);
    }

    /// <summary>
    /// Число ссылок элемента VIEW_DYN — 0x30: предупреждение с таблицей и смещением; Height Permanent area — <c>?</c>,
    /// остальные строки листа «Экраны» не меняются.
    /// </summary>
    [Fact]
    public void BrokenViewDyn_PermanentAreaHeightUnknown()
    {
        using var dir = new TestDirectory();
        // +0x04 — u32 число ссылок, см. ReferenceHeader
        var (input, item) = CorruptedCopy.U32(dir, "samples/pdata.fwc", "VIEW_DYN", 0, 0x04, 0x30);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        var rows = PermanentAreaHeightUnknown(run, dir, $"VIEW_DYN @ 0x{item.Offset:X}: запись 1: 48 ссылок не помещаются в {item.Length} байт");
        Assert.Equal("Root screen | 1 | Yes | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen", rows[2]);
    }

    /// <summary>
    /// Число ссылок элемента VIEW_DYN — 3: ссылки помещаются, высота за ними — нет; предупреждение с таблицей и смещением,
    /// Height Permanent area — <c>?</c>.
    /// </summary>
    [Fact]
    public void ShortViewDyn_HeightDoesNotFit()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U32(dir, "samples/pdata.fwc", "VIEW_DYN", 0, 0x04, 3);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        PermanentAreaHeightUnknown(run, dir, $"VIEW_DYN @ 0x{item.Offset:X}: запись 1: высота Permanent area не помещается в {item.Length} байт");
    }

    /// <summary>
    /// Пара VIEW_DYN в объектах записи <c>Overview</c> (SCREEN, запись 1) ведёт на другую таблицу или за последний
    /// элемент: предупреждение со смещением таблицы VIEW_DYN, Height Permanent area — <c>?</c>.
    /// </summary>
    [Theory]
    // в samples/pdata.fwc третья пара объектов Overview (0, VIEW_DYN) — на +0x44: номер элемента, на +0x46 — Id таблицы
    [InlineData(0x46, 6, "запись Overview таблицы SCREEN не ссылается на VIEW_DYN, высота Permanent area не известна")]
    [InlineData(0x44, 1, "запись Overview ссылается на элемент 2, а элементов 1")]
    public void OverviewWithoutViewDynLink_HeightUnknown(int offset, ushort value, string message)
    {
        using var dir = new TestDirectory();
        var (input, _) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "SCREEN", 0, offset, value);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        var table = run.Document!.FindTable("VIEW_DYN")!;
        PermanentAreaHeightUnknown(run, dir, $"VIEW_DYN @ 0x{table.Offset:X}: {message}");
    }

    /// <summary>
    /// Первое слово записи <c>Overview</c> (SCREEN, запись 1) — 7: предупреждение только от SCREEN; записи Overview в
    /// документе нет — VIEW_DYN не читается и своего предупреждения не добавляет, высота не известна.
    /// </summary>
    [Fact]
    public void BrokenOverviewRecord_NoSecondViewDynWarning()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U16(dir, "samples/pdata.fwc", "SCREEN", 0, 0, 7);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SCREEN @ 0x{item.Offset:X}: запись 1: первое слово 7, ожидалось 3 или 1");
        Assert.DoesNotContain("VIEW_DYN", run.Errors);
        Assert.Null(run.Document!.PermanentAreaHeight);
    }

    /// <summary>
    /// Общая проверка сбоя высоты Permanent area: предупреждение <c>не разобрано - &lt;сообщение&gt;</c> без stack trace,
    /// на листе «Экраны» у Permanent area Height — <c>?</c>.
    /// </summary>
    /// <returns>Слепок листа «Экраны» для остальных проверок теста.</returns>
    private static List<string> PermanentAreaHeightUnknown(PanelRun run, TestDirectory dir, string message)
    {
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - {message}");
        RunAssertions.NoStackTrace(run);
        var rows = SheetDump.Read(dir.PanelDataWorkbook, "Экраны");
        Assert.Equal("Permanent area |  |  | 800 | ? | 182, 182, 182 |  |  |  |  |  | Permanent area", rows[1]);
        return rows;
    }

    /// <summary>
    /// Число ссылок элемента GUI — 0x30: предупреждение; «Стартовый экран: неизвестно», в колонке
    /// Start screen нет <c>Yes</c>, номера экранов остаются.
    /// </summary>
    [Fact]
    public void BrokenGui_StartScreenUnknown()
    {
        using var dir = new TestDirectory();
        // +0x04 — u32 число ссылок, см. ReferenceHeader
        var (input, item) = CorruptedCopy.U32(dir, "samples/pdata.fwc", "GUI", 0, 0x04, 0x30);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - GUI @ 0x{item.Offset:X}: запись 1: 48 ссылок не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Экранов: 1");
        RunAssertions.HasLine(run.Output, "Стартовый экран: неизвестно");
        var rows = SheetDump.Read(dir.PanelDataWorkbook, "Экраны");
        Assert.Contains("Root screen | 1 |  | 800 | 420 | 182, 182, 182 |  |  |  |  |  | Screen", rows);
        Assert.DoesNotContain(rows, r => r.Contains(" | Yes | "));
    }

    /// <summary>Записать u16 в элемент таблицы уже испорченной копии — второе повреждение той же копии.</summary>
    /// <returns>Испорченный элемент (смещение — для текста предупреждения).</returns>
    private static TableItem WriteU16(string path, string table, int index, int offset, ushort value)
    {
        var doc = FwxReader.Read(path);
        var item = FwxReader.Items(doc.Binary, doc.FindTable(table)!).ElementAt(index);
        var bytes = File.ReadAllBytes(path);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)item.Offset + offset), value);
        File.WriteAllBytes(path, bytes);
        return item;
    }
}
