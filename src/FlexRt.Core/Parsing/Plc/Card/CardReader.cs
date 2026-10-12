using FlexRt.Binary;
using FlexRt.Model.Plc.Card;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>
/// Карта памяти ПЛК S7-1500 (копия папки карты: <c>SIMATIC.S7S\OMSSTORE</c>): теги ПЛК и
/// блоки данных в той же модели <see cref="PlcProject"/>, что даёт PEData.plf.
/// </summary>
public static class CardReader
{
    /// <summary>ID единственного ПЛК карты в модели.</summary>
    private const long PlcId = 1;

    /// <summary>Папка хранилища объектов внутри папки карты.</summary>
    private static readonly string StorePath = Path.Combine("SIMATIC.S7S", "OMSSTORE");

    /// <summary>
    /// Прочитать карту. ПЛК один; имя, модель CPU и IP интерфейсов CPU — из конфигурации
    /// оборудования на карте (<see cref="CardHardware"/>, <see cref="CardNetwork"/>); нет имени
    /// CPU — имя папки карты.
    /// Тег или DB, который не удалось разобрать, попадает в <see cref="PlcProject.Problems"/>,
    /// остальные читаются.
    /// </summary>
    /// <returns>ПЛК, теги и блоки данных карты.</returns>
    /// <exception cref="DirectoryNotFoundException">В папке нет <c>SIMATIC.S7S\OMSSTORE</c>.</exception>
    /// <exception cref="FwxFormatException">Сжатый поток или XML таблицы тегов не разбирается.</exception>
    /// <exception cref="IOException">Файл карты не удалось прочитать.</exception>
    public static PlcProject Read(string directory)
    {
        var store = Path.Combine(directory, StorePath);
        if (!Directory.Exists(store))
        {
            throw new DirectoryNotFoundException($"в папке нет {StorePath}");
        }
        var streams = new List<CardStream>();
        var addresses = new List<uint>();
        var cpu = ReadStore(store, streams, addresses);
        var project = new PlcProject();
        var device = new PlcDevice(PlcId, cpu?.Name ?? DirectoryName(directory)) { Model = cpu?.Model };
        // флаг подсети на карте не читается: адреса CPU считаются подключёнными (у карты один ПЛК, флаг не используется)
        device.Addresses.AddRange(addresses.Select(ip => (ip, true)));
        project.Devices.Add(device);
        CardTagParser.ReadAll(streams, PlcId, project);
        CardDbParser.ReadAll(streams, PlcId, project);
        return project;
    }

    /// <summary>
    /// Прочитать каждый файл хранилища один раз, в порядке путей, и разобрать его байты: потоки
    /// (<see cref="CardStore.ReadFile"/>), CPU (<see cref="CardHardware.FindCpu"/>, первый найденный)
    /// и IP интерфейсов CPU (<see cref="CardNetwork.AddCpuAddresses"/>).
    /// </summary>
    /// <returns>CPU или <c>null</c>, если ни в одном файле его нет.</returns>
    /// <exception cref="FwxFormatException">Поток с известным словарём не распаковывается.</exception>
    /// <exception cref="IOException">Файл карты не удалось прочитать.</exception>
    private static CardCpu? ReadStore(string store, List<CardStream> streams, List<uint> addresses)
    {
        CardCpu? cpu = null;
        foreach (var path in Directory.EnumerateFiles(store, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var data = File.ReadAllBytes(path);
            CardStore.ReadFile(data, Path.GetRelativePath(store, path), streams);
            cpu ??= CardHardware.FindCpu(data);
            CardNetwork.AddCpuAddresses(data, addresses);
        }
        return cpu;
    }

    /// <summary>Имя папки карты: последний элемент полного пути, в том числе при завершающем разделителе.</summary>
    /// <returns>Имя папки.</returns>
    private static string DirectoryName(string directory) => Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)));
}
