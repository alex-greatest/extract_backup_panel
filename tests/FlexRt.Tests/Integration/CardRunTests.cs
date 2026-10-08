using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Интеграционные тесты на карту ПЛК вместо PEData.plf: <c>samples/card/pdata.fwc</c> (панель
/// TP1500 Comfort, TIA V17, бэкап CCB-A13) и <c>samples/card/CCB-A03.zip</c> (копия карты
/// S7-1500F с данными TIA V19). Испорченные карты — копии, изменённые в рантайме во временном
/// каталоге теста (<see cref="TestDirectory"/>); <c>samples/</c> не меняется.
/// </summary>
public sealed class CardRunTests
{
    /// <summary>Панель, связанная с картой.</summary>
    private const string Panel = "samples/card/pdata.fwc";

    /// <summary>
    /// Копия карты ПЛК одним архивом (128 файлов карты не засоряют репозиторий); тест распаковывает
    /// её во временный каталог. Имя папки в архиве — имя ПЛК в листе «Теги».
    /// </summary>
    private const string CardArchive = "samples/card/CCB-A03.zip";

    /// <summary>Папка карты внутри архива.</summary>
    private const string CardFolder = "CCB-A03";

    /// <summary>
    /// ID словаря интерфейса блока (<c>DebugInfo_IntfDesc_98000001</c>) после заголовка zlib
    /// <c>78 7d</c>: начало сжатого интерфейса DB, FB или UDT.
    /// </summary>
    private static readonly byte[] InterfaceStreamStart = [0x78, 0x7d, 0x66, 0x05, 0x2b, 0x13];

    /// <summary>
    /// Панель с картой: код 0, сводка «карта ПЛК; ПЛК: 1, тегов ПЛК: 591», лист «Сводка» первый —
    /// модель панели и версия Runtime из файлов рядом с <c>pdata.fwc</c> (копии из бэкапа CCB-A13),
    /// IP панели, соединение, модель CPU с карты; лист «Теги» совпадает с эталоном (481 тег найден, 18 — «отсутствует в файле ПЛК»: программу ПЛК меняли после
    /// сборки панели). Эталон — слепок вывода программы, сверенный только по именам HMI-тегов
    /// (проекта TIA карты нет): тест ловит регрессии, а не подтверждает типы и комментарии.
    /// </summary>
    [Fact]
    public void CardPanel_WithCard_MatchesExpected()
    {
        using var dir = new TestDirectory();
        var card = dir.ExtractFromRepo(CardArchive, CardFolder);
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(Panel), card, dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Тегов: 517");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {card} (карта ПЛК; ПЛК: 1, тегов ПЛК: 591)");
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/card/теги.txt");
        SummarySheetDump.Contains(dir.PanelDataWorkbook,
            "Модель панели=TP1500 Comfort V2",
            "Версия Runtime=17.00.00.07",
            "IP панели=192.168.1.3 / 255.255.255.0",
            "Языки панели=0x419",
            "Строк языков=19674",
            "Тегов панели=517",
            "Соединение HMI1=IP ПЛК 192.168.1.1",
            $"Данные ПЛК=карта ПЛК: {card}",
            "ПЛК=CCB-A03",
            "Модель CPU=CPU 1515F-2 PN (6ES7 515-2FN03-0AB0)",
            "IP ПЛК (из данных ПЛК)=неизвестно",
            "Тегов ПЛК=591",
            "Блоков данных (DB)=95");
    }

    /// <summary>
    /// Путь карты с завершающим разделителем: та же карта, имя ПЛК — имя папки (<c>CCB-A03</c>), код 0.
    /// </summary>
    [Fact]
    public void CardPathWithTrailingSeparator_SameResult()
    {
        using var dir = new TestDirectory();
        var card = dir.ExtractFromRepo(CardArchive, CardFolder) + Path.DirectorySeparatorChar;
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(Panel), card, dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.TagSheetEquals(dir.PanelDataWorkbook, "samples/expected/card/теги.txt");
    }

    /// <summary>
    /// Папка без <c>SIMATIC.S7S\OMSSTORE</c>: карта не прочитана — код 1, предупреждение в stderr
    /// и на листе «Ошибки», строка «не прочитан» в stdout, без трассы стека.
    /// </summary>
    [Fact]
    public void FolderWithoutStore_WarnsAndCardNotRead()
    {
        using var dir = new TestDirectory();
        var card = Path.Combine(dir.Root, "пустая-карта");
        Directory.CreateDirectory(card);
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(Panel), card, dir.OutDir);

        var message = $"{card}: в папке нет {Path.Combine("SIMATIC.S7S", "OMSSTORE")}";
        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.StdErr, $"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {message}");
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {card} не прочитан - данные ПЛК: неизвестно");
        RunAssertions.ErrorsSheetContains(dir.PanelDataWorkbook, message);
        RunAssertions.NoStackTrace(run);
    }

    /// <summary>
    /// Копия карты с инвертированным байтом внутри первого сжатого интерфейса блока: поток не
    /// распаковывается, карта не прочитана целиком — код 1, сообщение с файлом и смещением потока
    /// в stderr и на листе «Ошибки», колонки ПЛК «неизвестно».
    /// </summary>
    [Fact]
    public void BrokenInterfaceStream_CardNotRead()
    {
        using var dir = new TestDirectory();
        var card = dir.ExtractFromRepo(CardArchive, CardFolder);
        var (file, offset) = CorruptFirstInterfaceStream(card);
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(Panel), card, dir.OutDir);

        var message = $"{card}: {file} @ 0x{offset:X}: поток zlib";
        RunAssertions.ExitCode(run, 1);
        Assert.Contains(run.StdErr.Split('\n'), line => line.StartsWith($"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {message}", StringComparison.Ordinal));
        RunAssertions.HasLine(run.StdOut, $"Файл ПЛК: {card} не прочитан - данные ПЛК: неизвестно");
        RunAssertions.ErrorsSheetContains(dir.PanelDataWorkbook, message);
        Assert.Contains(TagSheetDump.Read(dir.PanelDataWorkbook), row => row.StartsWith("A13=GlobalDB_bResetAlarmCmd | B13=Bool | C13=HMI1 | D13=неизвестно | E13=неизвестно |", StringComparison.Ordinal));
        RunAssertions.NoStackTrace(run);
    }

    /// <summary>
    /// Найти первый сжатый интерфейс блока в файлах <c>OMSSTORE</c> (в порядке путей, как их
    /// читает программа) и инвертировать первый байт данных deflate — сразу за 6 байтами заголовка
    /// со словарём, поэтому байт всегда внутри потока. Проверено на этой карте: любой одиночный
    /// инвертированный байт первого потока (0x1c9 байт) ломает распаковку.
    /// </summary>
    /// <returns>Файл относительно <c>OMSSTORE</c> и смещение потока.</returns>
    private static (string File, int Offset) CorruptFirstInterfaceStream(string card)
    {
        var store = Path.Combine(card, "SIMATIC.S7S", "OMSSTORE");
        foreach (var path in Directory.EnumerateFiles(store, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var bytes = File.ReadAllBytes(path);
            var offset = bytes.AsSpan().IndexOf(InterfaceStreamStart);
            if (offset < 0 || offset + InterfaceStreamStart.Length >= bytes.Length)
            {
                continue;
            }
            bytes[offset + InterfaceStreamStart.Length] ^= 0xff;
            File.WriteAllBytes(path, bytes);
            return (Path.GetRelativePath(store, path), offset);
        }
        throw new InvalidOperationException("в копии карты нет сжатого интерфейса блока");
    }
}
