using FlexRt.Binary;
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
    /// Прочитать карту. ПЛК один, его имя — имя папки карты: своего имени ПЛК на карте нет.
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
        var streams = CardStore.Read(store);
        var project = new PlcProject();
        project.Devices.Add(new PlcDevice(PlcId, DirectoryName(directory)) { Model = CardHardware.CpuModel(store) });
        CardTagParser.ReadAll(streams, PlcId, project);
        CardDbParser.ReadAll(streams, PlcId, project);
        return project;
    }

    /// <summary>Имя папки карты: последний элемент полного пути, в том числе при завершающем разделителе.</summary>
    /// <returns>Имя папки.</returns>
    private static string DirectoryName(string directory) => Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)));
}
