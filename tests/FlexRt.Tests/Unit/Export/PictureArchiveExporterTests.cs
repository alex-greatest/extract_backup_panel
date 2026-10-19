using System.IO.Compression;
using FlexRt.Export.Pictures;
using FlexRt.Parsing.Panel;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>
/// Архив картинок окна (<see cref="PictureArchiveExporter"/>) на <c>samples/a603/pdata.fwc</c> (72 PNG и 4 JPEG): в архиве те
/// же файлы, что консоль пишет в папку <c>images</c> (<see cref="PictureExporter"/>), в порядке ядра, в корне, без сжатия.
/// </summary>
public sealed class PictureArchiveExporterTests
{
    /// <summary>Образец с PNG и JPEG.</summary>
    private const string A603Panel = "samples/a603/pdata.fwc";

    /// <summary>Записи архива — файлы папки <c>images</c> консоли: те же имена в порядке ядра и те же байты, без сжатия.</summary>
    [Fact]
    public void Archive_SameFilesAsConsoleImages()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo(A603Panel));
        using var dir = new TestDirectory();
        var zip = Path.Combine(dir.Root, PictureArchiveExporter.FileName);
        PictureExporter.Export(doc, dir.PicturesDir);

        PictureArchiveExporter.Export(doc.Pictures, zip);

        using var archive = ZipFile.OpenRead(zip);
        Assert.Equal(doc.Pictures.Select(PictureExporter.FileName), archive.Entries.Select(e => e.FullName));
        Assert.All(archive.Entries, entry =>
        {
            Assert.Equal(entry.Length, entry.CompressedLength);
            using var stream = entry.Open();
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            Assert.Equal(File.ReadAllBytes(Path.Combine(dir.PicturesDir, entry.FullName)), bytes.ToArray());
        });
    }

    /// <summary>Архив на месте прежнего файла: старое содержимое не остаётся.</summary>
    [Fact]
    public void ExistingFile_Overwritten()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo(A603Panel));
        using var dir = new TestDirectory();
        var zip = Path.Combine(dir.Root, PictureArchiveExporter.FileName);
        File.WriteAllBytes(zip, new byte[1 << 20]);

        PictureArchiveExporter.Export([.. doc.Pictures.Take(1)], zip);

        using var archive = ZipFile.OpenRead(zip);
        Assert.Equal(["PICTURE_1.png"], archive.Entries.Select(e => e.FullName));
    }
}
