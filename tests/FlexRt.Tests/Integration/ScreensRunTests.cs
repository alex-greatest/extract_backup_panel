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
    /// <summary>Заголовок листа «Экраны».</summary>
    private const string Header = "Name | Number | Start screen | Width | Height | Вид";

    /// <summary>
    /// <c>samples/pdata.fwc</c>: Permanent area (запись <c>Overview</c>), <c>Root screen</c> — номер 1 и стартовый,
    /// четыре slide-in TP700 Comfort. «Экранов: 1», «Стартовый экран: Root screen (1)» в тексте запуска и на «Сводке», код 0.
    /// </summary>
    [Fact]
    public void SamplePanel_ScreensSheetAndStartScreen()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.Output, "Экранов: 1");
        RunAssertions.HasLine(run.Output, "Стартовый экран: Root screen (1)");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Экранов=1", "Стартовый экран=Root screen (1)");
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 480 | Permanent area",
            "Root screen | 1 | Yes | 800 | 420 | Screen",
            "Slide-in screen left |  |  | 267 | 480 | Slide-in screen",
            "Slide-in screen bottom |  |  | 800 | 160 | Slide-in screen",
            "Slide-in screen right |  |  | 267 | 480 | Slide-in screen",
            "Slide-in screen top |  |  | 800 | 160 | Slide-in screen"
        ], ScreensSheetDump.Read(dir.PanelDataWorkbook));
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
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/screens/05_popup.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.Output, "Экранов: 3");
        RunAssertions.HasLine(run.Output, "Стартовый экран: Screen_2 (5)");
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 480 | Permanent area",
            "Root screen | 1 |  | 800 | 420 | Screen",
            "Screen_1 | 2 |  | 800 | 420 | Screen",
            "Screen_2 | 5 | Yes | 800 | 420 | Screen",
            "Pop-up screen_1 |  |  | 400 | 200 | Pop-up screen",
            "Slide-in screen top |  |  | 800 | 160 | Slide-in screen",
            "Slide-in screen left |  |  | 267 | 480 | Slide-in screen",
            "Slide-in screen bottom |  |  | 800 | 160 | Slide-in screen",
            "Slide-in screen right |  |  | 267 | 480 | Slide-in screen"
        ], ScreensSheetDump.Read(dir.PanelDataWorkbook));
    }

    /// <summary>
    /// A603A0097 (TIA V21, TP1900 Comfort): 49 экранов с номерами, стартовый <c>998_LOGIN</c> (998), один pop-up и
    /// четыре slide-in.
    /// </summary>
    [Fact]
    public void A603_StartScreenByNumber()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/a603/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.Output, "Экранов: 49");
        RunAssertions.HasLine(run.Output, "Стартовый экран: 998_LOGIN (998)");
        var rows = ScreensSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal(1 + 1 + 49 + 1 + 4, rows.Count);
        Assert.Equal("Permanent area |  |  | 1366 | 768 | Permanent area", rows[1]);
        Assert.Equal("001_AUTO_SCREEN | 1 |  | 1366 | 768 | Screen", rows[2]);
        Assert.Equal("998_LOGIN | 998 | Yes | 1366 | 768 | Screen", rows[49]);
        Assert.Equal("No_Authorization_Msg_PopUpScreen |  |  | 800 | 60 | Pop-up screen", rows[51]);
        Assert.Single(rows, r => r.Contains(" | Yes | "));
    }

    /// <summary>
    /// Бэкап CCB-A13 (Runtime V17, TP1500 Comfort): 10 экранов с номерами, стартовый <c>001_MainScreen</c> (1),
    /// семь pop-up 636×322 (<c>UZ70</c>…<c>UZ90</c>) — на каждый в SCREEN своя служебная запись.
    /// </summary>
    [Fact]
    public void Card_SevenPopups()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/card/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.Output, "Экранов: 10");
        RunAssertions.HasLine(run.Output, "Стартовый экран: 001_MainScreen (1)");
        var rows = ScreensSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Equal("001_MainScreen | 1 | Yes | 1280 | 800 | Screen", rows[2]);
        Assert.Equal(["UZ70", "UZ60", "UZ40", "HPU", "UZ10", "UZ72", "UZ90"],
            rows.Where(r => r.EndsWith(" | 636 | 322 | Pop-up screen")).Select(r => r.Split(" | ")[0]));
    }

    /// <summary>
    /// Первое слово третьей записи SCREEN — 7: код 1, предупреждение с таблицей и смещением, две записи до сбоя
    /// (Permanent area и <c>Root screen</c>) остаются на листе.
    /// </summary>
    [Fact]
    public void BrokenScreenRecord_WarnsAndKeepsScreensBefore()
    {
        using var dir = new TestDirectory();
        var item = Corrupt(dir, "SCREEN", 2, 0, 7);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - SCREEN @ 0x{item.Offset:X}: запись 3: первое слово 7, ожидалось 3 или 1");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Стартовый экран: Root screen (1)");
        Assert.Equal(
        [
            Header,
            "Permanent area |  |  | 800 | 480 | Permanent area",
            "Root screen | 1 | Yes | 800 | 420 | Screen"
        ], ScreensSheetDump.Read(dir.PanelDataWorkbook));
    }

    /// <summary>
    /// Число пар элемента ADAPTVALUECHANG со ссылкой на GUI — 0xffff: код 1, предупреждение; номера не известны —
    /// Number и вид <c>Root screen</c> <c>?</c>, «Экранов: 0», стартовый экран <c>неизвестно (1)</c>.
    /// </summary>
    [Fact]
    public void BrokenAdaptValueChang_NumbersUnknown()
    {
        using var dir = new TestDirectory();
        // одна ссылка (на GUI): ссылки кончаются на +0x10, за ними u16 и u16 число пар
        var item = Corrupt(dir, "ADAPTVALUECHANG", 0, 0x12, 0xffff);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - ADAPTVALUECHANG @ 0x{item.Offset:X}: запись 1: 65535 пар не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Экранов: 0");
        RunAssertions.HasLine(run.Output, "Стартовый экран: неизвестно (1)");
        var rows = ScreensSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Contains("Root screen | ? |  | 800 | 420 | ?", rows);
        Assert.Contains("Slide-in screen top | ? |  | 800 | 160 | Slide-in screen", rows);
    }

    /// <summary>
    /// Число ссылок элемента GUI — 0x30: код 1, предупреждение; «Стартовый экран: неизвестно», в колонке
    /// Start screen нет <c>Yes</c>, номера экранов остаются.
    /// </summary>
    [Fact]
    public void BrokenGui_StartScreenUnknown()
    {
        using var dir = new TestDirectory();
        // +0x04 — u32 число ссылок, см. ReferenceHeader
        var item = Corrupt(dir, "GUI", 0, 0x04, 0x30);
        var run = FlexRtProcess.Run(Path.Combine(dir.Root, "pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - GUI @ 0x{item.Offset:X}: запись 1: 48 ссылок не помещаются в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Экранов: 1");
        RunAssertions.HasLine(run.Output, "Стартовый экран: неизвестно");
        var rows = ScreensSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Contains("Root screen | 1 |  | 800 | 420 | Screen", rows);
        Assert.DoesNotContain(rows, r => r.Contains(" | Yes | "));
    }

    /// <summary>
    /// Скопировать <c>samples/pdata.fwc</c> в каталог теста как <c>pdata.fwc</c> и записать u16
    /// <paramref name="value"/> (little endian) в элемент <paramref name="index"/> таблицы
    /// <paramref name="table"/> со смещением <paramref name="offset"/>.
    /// </summary>
    /// <returns>Изменённый элемент: смещение и длина до правки.</returns>
    private static TableItem Corrupt(TestDirectory dir, string table, int index, int offset, ushort value)
    {
        var input = dir.CopyFromRepo("samples/pdata.fwc", "pdata.fwc");
        var doc = FwxReader.Read(input);
        var item = FwxReader.Items(doc.Binary, doc.FindTable(table)!).ElementAt(index);
        var bytes = File.ReadAllBytes(input);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan((int)item.Offset + offset), value);
        File.WriteAllBytes(input, bytes);
        return item;
    }
}
