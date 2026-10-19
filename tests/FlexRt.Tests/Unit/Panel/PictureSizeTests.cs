using FlexRt.Model.Panel.Pictures;
using FlexRt.Parsing.Panel;
using FlexRt.Parsing.Panel.Pictures;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Unit.Panel;

/// <summary>
/// Ширина и высота картинки из заголовка файла (<see cref="PictureSize"/>). Картинки — из <c>samples/pdata.fwc</c> (12 PNG)
/// и <c>samples/a603/pdata.fwc</c> (72 PNG и 4 JPEG); ожидаемые размеры — те, что дал браузер окна, раскодировав файлы
/// (<c>naturalWidth</c> × <c>naturalHeight</c>). Случаи, которых в образцах нет (байты заполнения, маркеры без длины,
/// обрезанный файл), — копии настоящего JPEG, изменённые в тесте.
/// </summary>
public sealed class PictureSizeTests
{
    /// <summary>Образец с 12 PNG.</summary>
    private const string SamplePanel = "samples/pdata.fwc";

    /// <summary>Образец с 72 PNG и 4 JPEG.</summary>
    private const string A603Panel = "samples/a603/pdata.fwc";

    /// <summary>Картинки образцов по пути: каждый образец читается один раз.</summary>
    private static readonly Dictionary<string, Lazy<List<PanelPicture>>> Panels = new(StringComparer.Ordinal)
    {
        [SamplePanel] = new Lazy<List<PanelPicture>>(() => Read(SamplePanel)),
        [A603Panel] = new Lazy<List<PanelPicture>>(() => Read(A603Panel))
    };

    /// <summary>Размеры PNG и JPEG совпадают с раскодированными браузером.</summary>
    [Theory]
    [InlineData(SamplePanel, "PICTURE", 1, 107, 60)]
    [InlineData(SamplePanel, "PICTURE", 5, 34, 34)]
    [InlineData(SamplePanel, "TILED_PICTURE", 2, 4, 16)]
    [InlineData(A603Panel, "PICTURE", 5, 483, 148)]
    [InlineData(A603Panel, "PICTURE", 16, 57, 57)]
    [InlineData(A603Panel, "PICTURE", 26, 48, 48)]
    [InlineData(A603Panel, "TILED_PICTURE", 28, 23, 43)]
    public void SamplePicture_SizeAsDecoded(string panel, string table, int number, int width, int height)
    {
        var picture = Pictures(panel).Single(p => p.Table == table && p.Index + 1 == number);

        Assert.Equal((width, height), PictureSize.Read(picture.Data, picture.Extension));
    }

    /// <summary>Размер прочитан у каждой картинки обоих образцов, PNG и JPEG.</summary>
    [Fact]
    public void AllSamplePictures_SizeRead()
    {
        var all = Pictures(SamplePanel).Concat(Pictures(A603Panel)).ToList();

        Assert.Equal(88, all.Count);
        Assert.Equal(4, all.Count(p => p.Extension == "jpg"));
        Assert.All(all, p => Assert.NotNull(PictureSize.Read(p.Data, p.Extension)));
    }

    /// <summary>
    /// Перед маркером после SOI — байты заполнения <c>FF</c> или маркер без длины (TEM <c>FF 01</c>, RST0
    /// <c>FF D0</c>): они пропускаются, размер тот же, что у исходного JPEG.
    /// </summary>
    [Theory]
    [InlineData(new byte[] { 0xFF })]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF })]
    [InlineData(new byte[] { 0xFF, 0x01 })]
    [InlineData(new byte[] { 0xFF, 0xD0 })]
    public void Jpeg_FillBytesAndStandaloneMarkers_Skipped(byte[] inserted)
    {
        var jpeg = Jpeg16();

        byte[] changed = [.. jpeg.AsSpan(0, 2), .. inserted, .. jpeg.AsSpan(2)];

        Assert.Equal((57, 57), PictureSize.Read(changed, "jpg"));
    }

    /// <summary>JPEG, обрезанный до маркера SOF, — <c>null</c>, а не исключение.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(20)]
    public void Jpeg_TruncatedBeforeSof_Null(int length)
    {
        Assert.Null(PictureSize.Read(Jpeg16()[..length], "jpg"));
    }

    /// <summary>SOS раньше SOF — размер не ищется в сжатых данных: <c>null</c>.</summary>
    [Fact]
    public void Jpeg_ScanBeforeFrame_Null()
    {
        Assert.Null(PictureSize.Read([0xFF, 0xD8, 0xFF, 0xDA, 0x00, 0x02, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x39, 0x00, 0x39], "jpg"));
    }

    /// <summary>PNG короче блока IHDR — <c>null</c>, а не исключение.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(23)]
    public void Png_ShorterThanIhdr_Null(int length)
    {
        var png = Pictures(SamplePanel).First(p => p.Extension == "png").Data;

        Assert.Null(PictureSize.Read(png[..length], "png"));
    }

    /// <summary>Байты <c>PICTURE_16.jpg</c> из <c>samples/a603/pdata.fwc</c> (57 × 57).</summary>
    /// <returns>Файл JPEG.</returns>
    private static byte[] Jpeg16() => Pictures(A603Panel).Single(p => p is { Table: "PICTURE", Index: 15 }).Data;

    /// <summary>Картинки образца по пути из теста.</summary>
    /// <returns>Картинки в порядке ядра.</returns>
    /// <exception cref="ArgumentException">Образца нет в <see cref="Panels"/> — его нужно добавить, а не подменять другим.</exception>
    private static List<PanelPicture> Pictures(string panel) =>
        Panels.TryGetValue(panel, out var pictures) ? pictures.Value : throw new ArgumentException($"образца {panel} нет в списке теста", nameof(panel));

    /// <summary>Прочитать картинки панели <paramref name="relative"/> из репозитория.</summary>
    /// <returns>Картинки в порядке ядра.</returns>
    private static List<PanelPicture> Read(string relative) => FwxReader.Read(RepositoryPaths.InRepo(relative)).Pictures;
}
