using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Картинки панели (PICTURE, TILED_PICTURE): файлы в папке <c>images</c>, строка «Картинок» в тексте запуска и на
/// «Сводке». Испорченный вход — копия <c>samples/pdata.fwc</c>, изменённая в рантайме во временном каталоге теста.
/// </summary>
public sealed class PicturesRunTests
{
    /// <summary>Сигнатура PNG.</summary>
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    /// <summary>Сигнатура JPEG.</summary>
    private static readonly byte[] JpegSignature = [0xff, 0xd8, 0xff];

    /// <summary>Последние 8 байт PNG: тип чанка IEND и его CRC.</summary>
    private static readonly byte[] IendChunkTail = [0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82];

    /// <summary>
    /// <c>samples/pdata.fwc</c>: 8 картинок PICTURE и 4 TILED_PICTURE, все PNG; «Картинок: 12» в тексте запуска и на
    /// «Сводке», строка экспорта с путём папки <c>images</c>, код 0. Каждый файл — PNG, кончается чанком IEND.
    /// </summary>
    [Fact]
    public void SamplePanel_WritesPictureFiles()
    {
        var (dir, run) = SharedRuns.WithoutPlc("samples/pdata.fwc");

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.Output, "Картинок: 12");
        RunAssertions.HasLine(run.Output, $"[Картинки -> файлы] 12 картинок -> {dir.PicturesDir}");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Картинок=12");
        Assert.Equal(
        [
            "PICTURE_1.png", "PICTURE_2.png", "PICTURE_3.png", "PICTURE_4.png",
            "PICTURE_5.png", "PICTURE_6.png", "PICTURE_7.png", "PICTURE_8.png",
            "TILED_PICTURE_1.png", "TILED_PICTURE_2.png", "TILED_PICTURE_3.png", "TILED_PICTURE_4.png"
        ], FileNames(dir.PicturesDir));
        Assert.All(Directory.GetFiles(dir.PicturesDir), path =>
        {
            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.AsSpan().StartsWith(PngSignature), $"{path}: не PNG");
            Assert.True(bytes.AsSpan().EndsWith(IendChunkTail), $"{path}: нет IEND в конце");
        });
    }

    /// <summary>
    /// A603A0097 (TIA V21, TP1900 Comfort): 48 PICTURE и 28 TILED_PICTURE, из них 4 JPEG (PICTURE 16, 17, 18, 26);
    /// «Картинок: 76», код 0.
    /// </summary>
    [Fact]
    public void A603_PngAndJpeg()
    {
        var (dir, run) = SharedRuns.WithoutPlc("samples/a603/pdata.fwc");

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.Output, "Картинок: 76");
        var names = FileNames(dir.PicturesDir);
        Assert.Equal(76, names.Count);
        Assert.Equal(["PICTURE_16.jpg", "PICTURE_17.jpg", "PICTURE_18.jpg", "PICTURE_26.jpg"], names.Where(n => n.EndsWith(".jpg")));
        Assert.True(File.ReadAllBytes(Path.Combine(dir.PicturesDir, "PICTURE_16.jpg")).AsSpan().StartsWith(JpegSignature), "PICTURE_16.jpg: не JPEG");
    }

    /// <summary>
    /// Длина картинки третьего элемента PICTURE — 0xffff: код 1, предупреждение с таблицей и смещением; две
    /// картинки PICTURE до сбоя и все четыре TILED_PICTURE записаны, «Картинок: 6».
    /// </summary>
    [Fact]
    public void BrokenPictureLength_WarnsAndKeepsPicturesBefore()
    {
        using var dir = new TestDirectory();
        // +0x0c — u32 длина картинки, см. PictureParser
        var (input, item) = CorruptedCopy.U32(dir, "samples/pdata.fwc", "PICTURE", 2, 0x0c, 0xffff);
        var run = FlexRtProcess.Run(input, dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 1);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - PICTURE @ 0x{item.Offset:X}: запись 3: картинка 65535 байт не помещается в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Картинок: 6");
        Assert.Equal(
        [
            "PICTURE_1.png", "PICTURE_2.png",
            "TILED_PICTURE_1.png", "TILED_PICTURE_2.png", "TILED_PICTURE_3.png", "TILED_PICTURE_4.png"
        ], FileNames(dir.PicturesDir));
    }

    /// <summary>Имена файлов каталога в порядке ordinal.</summary>
    /// <returns>Имена без пути.</returns>
    private static List<string> FileNames(string dir) =>
        [.. Directory.GetFiles(dir).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];
}
