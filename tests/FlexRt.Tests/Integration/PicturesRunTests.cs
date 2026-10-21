using System.IO.Compression;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Картинки панели (PICTURE, TILED_PICTURE): записи архива <c>images.zip</c>, строка «Картинок» в тексте запуска и на
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
    /// «Сводке», без предупреждений. Каждая запись архива — PNG, кончается чанком IEND.
    /// </summary>
    [Fact]
    public void SamplePanel_WritesPictureFiles()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), null, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Картинок: 12");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Картинок=12");
        var archive = PanelRunner.SavePictures(run, dir);
        Assert.Equal(
        [
            "PICTURE_1.png", "PICTURE_2.png", "PICTURE_3.png", "PICTURE_4.png",
            "PICTURE_5.png", "PICTURE_6.png", "PICTURE_7.png", "PICTURE_8.png",
            "TILED_PICTURE_1.png", "TILED_PICTURE_2.png", "TILED_PICTURE_3.png", "TILED_PICTURE_4.png"
        ], FileNames(archive));
        Assert.All(Files(archive), file =>
        {
            Assert.True(file.Bytes.AsSpan().StartsWith(PngSignature), $"{file.Name}: не PNG");
            Assert.True(file.Bytes.AsSpan().EndsWith(IendChunkTail), $"{file.Name}: нет IEND в конце");
        });
    }

    /// <summary>
    /// A603A0097 (TIA V21, TP1900 Comfort): 48 PICTURE и 28 TILED_PICTURE, из них 4 JPEG (PICTURE 16, 17, 18, 26);
    /// «Картинок: 76», без предупреждений.
    /// </summary>
    [Fact]
    public void A603_PngAndJpeg()
    {
        using var dir = new TestDirectory();
        var run = PanelRunner.Run(RepositoryPaths.InRepo("samples/a603/pdata.fwc"), null, dir.OutDir);

        RunAssertions.Clean(run);
        RunAssertions.HasLine(run.Output, "Картинок: 76");
        var archive = PanelRunner.SavePictures(run, dir);
        var names = FileNames(archive);
        Assert.Equal(76, names.Count);
        Assert.Equal(["PICTURE_16.jpg", "PICTURE_17.jpg", "PICTURE_18.jpg", "PICTURE_26.jpg"], names.Where(n => n.EndsWith(".jpg")));
        Assert.True(Files(archive).Single(f => f.Name == "PICTURE_16.jpg").Bytes.AsSpan().StartsWith(JpegSignature), "PICTURE_16.jpg: не JPEG");
    }

    /// <summary>
    /// Длина картинки третьего элемента PICTURE — 0xffff: предупреждение с таблицей и смещением; две
    /// картинки PICTURE до сбоя и все четыре TILED_PICTURE в архиве, «Картинок: 6».
    /// </summary>
    [Fact]
    public void BrokenPictureLength_WarnsAndKeepsPicturesBefore()
    {
        using var dir = new TestDirectory();
        // +0x0c — u32 длина картинки, см. PictureParser
        var (input, item) = CorruptedCopy.U32(dir, "samples/pdata.fwc", "PICTURE", 2, 0x0c, 0xffff);
        var run = PanelRunner.Run(input, null, dir.OutDir);

        RunAssertions.HasProblems(run);
        RunAssertions.HasLine(run.Errors, $"ПРЕДУПРЕЖДЕНИЕ: не разобрано - PICTURE @ 0x{item.Offset:X}: запись 3: картинка 65535 байт не помещается в {item.Length} байт");
        RunAssertions.NoStackTrace(run);
        RunAssertions.HasLine(run.Output, "Картинок: 6");
        Assert.Equal(
        [
            "PICTURE_1.png", "PICTURE_2.png",
            "TILED_PICTURE_1.png", "TILED_PICTURE_2.png", "TILED_PICTURE_3.png", "TILED_PICTURE_4.png"
        ], FileNames(PanelRunner.SavePictures(run, dir)));
    }

    /// <summary>Имена записей архива в порядке ordinal.</summary>
    /// <returns>Имена записей.</returns>
    private static List<string> FileNames(string archive)
    {
        using var zip = ZipFile.OpenRead(archive);
        return [.. zip.Entries.Select(e => e.FullName).Order(StringComparer.Ordinal)];
    }

    /// <summary>Записи архива по порядку: имя и байты.</summary>
    /// <returns>Записи архива.</returns>
    private static List<(string Name, byte[] Bytes)> Files(string archive)
    {
        using var zip = ZipFile.OpenRead(archive);
        return [.. zip.Entries.Select(e => (e.FullName, Read(e)))];
    }

    /// <summary>Байты записи архива.</summary>
    /// <returns>Содержимое записи.</returns>
    private static byte[] Read(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }
}
