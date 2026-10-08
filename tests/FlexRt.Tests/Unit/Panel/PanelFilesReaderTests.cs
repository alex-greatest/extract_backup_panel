using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Unit.Panel;

/// <summary>
/// Модель панели и версия Runtime из файлов рядом с <c>pdata.fwc</c> (<see cref="PanelFilesReader"/>).
/// Файлы — копии <c>samples/card/ProjectCharacteristics.rdf</c> и <c>BuildInfo.txt</c> (бэкап
/// CCB-A13) во временном каталоге, при необходимости изменённые в тесте.
/// </summary>
public sealed class PanelFilesReaderTests : IDisposable
{
    /// <summary>Смещение байта длины модели в <c>ProjectCharacteristics.rdf</c>.</summary>
    private const int ModelLengthOffset = 0x65;

    /// <summary>Временный каталог теста; удаляется в <see cref="Dispose"/>.</summary>
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"flexrt-panelfiles-{Guid.NewGuid():N}");

    /// <summary>Создаёт пустой временный каталог.</summary>
    public PanelFilesReaderTests() => Directory.CreateDirectory(_dir);

    /// <summary>Путь «pdata.fwc» во временном каталоге: сам файл не нужен, читаются соседние.</summary>
    private string PanelPath => Path.Combine(_dir, "pdata.fwc");

    /// <summary>Оба файла из бэкапа: модель и версия читаются.</summary>
    [Fact]
    public void BackupFiles_ModelAndRuntime()
    {
        CopySample("ProjectCharacteristics.rdf");
        CopySample("BuildInfo.txt");

        var files = PanelFilesReader.Read(PanelPath);

        Assert.Equal("TP1500 Comfort V2", files.Model);
        Assert.Equal("17.00.00.07", files.RuntimeVersion);
    }

    /// <summary>Нет файлов — оба значения <c>null</c>, без ошибки.</summary>
    [Fact]
    public void NoFiles_Null()
    {
        var files = PanelFilesReader.Read(PanelPath);

        Assert.Null(files.Model);
        Assert.Null(files.RuntimeVersion);
    }

    /// <summary>Байт длины модели 0, длина за концом файла или файл не с <c>RDF</c> — модели нет.</summary>
    [Theory]
    [InlineData(0x00, false)]
    [InlineData(0xff, false)]
    [InlineData(0x11, true)]
    public void BrokenRdf_NoModel(byte length, bool breakMagic)
    {
        var rdf = CopySample("ProjectCharacteristics.rdf");
        var bytes = File.ReadAllBytes(rdf);
        bytes[ModelLengthOffset] = length;
        if (breakMagic)
        {
            bytes[0] = (byte)'X';
        }
        File.WriteAllBytes(rdf, bytes);

        Assert.Null(PanelFilesReader.Read(PanelPath).Model);
    }

    /// <summary>В <c>BuildInfo.txt</c> нет строки <c>Build=</c> или в ней нет второй части — версии нет.</summary>
    [Theory]
    [InlineData("Comfort Runtime\r\nBRANCHNAME=x\r\n")]
    [InlineData("Build=2992\r\n")]
    [InlineData("Build=2992__05\r\n")]
    public void BuildInfoWithoutVersion_Null(string text)
    {
        File.WriteAllText(Path.Combine(_dir, "BuildInfo.txt"), text);

        Assert.Null(PanelFilesReader.Read(PanelPath).RuntimeVersion);
    }

    /// <summary>Удаляет временный каталог.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    /// <summary>Скопировать файл из <c>samples/card/</c> во временный каталог.</summary>
    /// <returns>Путь копии.</returns>
    private string CopySample(string name)
    {
        var target = Path.Combine(_dir, name);
        File.Copy(Integration.RepositoryPaths.InRepo($"samples/card/{name}"), target);
        return target;
    }
}
