using System.Text;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>
/// Файлы рядом с <c>pdata.fwc</c>: <c>ProjectCharacteristics.rdf</c> (есть и в бэкапе панели, и в
/// папке <c>Generates</c> проекта TIA) и <c>BuildInfo.txt</c> (только в бэкапе панели).
/// </summary>
public static class PanelFilesReader
{
    /// <summary>Начало <c>ProjectCharacteristics.rdf</c>.</summary>
    private static readonly byte[] RdfMagic = [.. "RDF"u8];

    /// <summary>
    /// Смещение байта длины модели панели в <c>ProjectCharacteristics.rdf</c>; строка — сразу за ним
    /// (наблюдение на трёх файлах: <c>11 "TP1500 Comfort V2"</c>, <c>0d "TP700 Comfort"</c>).
    /// </summary>
    private const int ModelLengthOffset = 0x65;

    /// <summary>Начало строки модели панели: сразу за байтом длины.</summary>
    private const int ModelStart = ModelLengthOffset + 1;

    /// <summary>Начало строки сборки в <c>BuildInfo.txt</c>: <c>Build=2992_17.00.00.07_05.01.0001</c>.</summary>
    private const string BuildPrefix = "Build=";

    /// <summary>
    /// Прочитать модель панели и версию Runtime. Нет файла, файл не читается или не того вида —
    /// значение <c>null</c>, без ошибки: эти файлы необязательны.
    /// </summary>
    /// <returns>Модель и версия.</returns>
    public static PanelFiles Read(string fwxPath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(fwxPath)) ?? "";
        return new PanelFiles(ReadModel(Path.Combine(dir, "ProjectCharacteristics.rdf")), ReadRuntime(Path.Combine(dir, "BuildInfo.txt")));
    }

    /// <summary>Модель панели: строка ASCII длиной из байта на <see cref="ModelLengthOffset"/>.</summary>
    /// <returns>Модель или <c>null</c>.</returns>
    private static string? ReadModel(string path)
    {
        var data = ReadOptional(path);
        if (data is null || !data.AsSpan().StartsWith(RdfMagic) || data.Length <= ModelLengthOffset)
        {
            return null;
        }
        var length = data[ModelLengthOffset];
        if (length == 0 || ModelStart + length > data.Length || data.AsSpan(ModelStart, length).IndexOfAnyExceptInRange((byte)0x20, (byte)0x7e) >= 0)
        {
            return null;
        }
        return Encoding.ASCII.GetString(data, ModelStart, length);
    }

    /// <summary>Версия Runtime: вторая часть строки <c>Build=</c> через <c>_</c> (<c>17.00.00.07</c>).</summary>
    /// <returns>Версия или <c>null</c>.</returns>
    private static string? ReadRuntime(string path)
    {
        var data = ReadOptional(path);
        if (data is null)
        {
            return null;
        }
        var line = Encoding.ASCII.GetString(data).Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith(BuildPrefix, StringComparison.Ordinal));
        var parts = line is null ? [] : line[BuildPrefix.Length..].Split('_');
        return parts is [_, { Length: > 0 } version, ..] ? version : null;
    }

    /// <summary>Байты необязательного файла.</summary>
    /// <returns>Содержимое или <c>null</c>, если файла нет или он не читается.</returns>
    private static byte[]? ReadOptional(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
