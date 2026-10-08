namespace FlexRt.Tests.Integration;

/// <summary>
/// Временный каталог одного интеграционного теста в <see cref="Path.GetTempPath"/> с
/// уникальным именем: в нём лежат испорченные копии входных файлов и каталог результата
/// программы (значение <c>FLEXRT_OUT</c>). Удаляется целиком в <see cref="Dispose"/>, поэтому
/// тесты не пишут в <c>data\out</c> репозитория и не мешают друг другу.
/// </summary>
public sealed class TestDirectory : IDisposable
{
    /// <summary>Создаёт пустой каталог <c>flexrt-tests-&lt;GUID&gt;</c> во временной папке.</summary>
    public TestDirectory()
    {
        Root = Path.Combine(Path.GetTempPath(), $"flexrt-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    /// <summary>Корень временного каталога.</summary>
    public string Root { get; }

    /// <summary>
    /// Каталог результата программы — значение <c>FLEXRT_OUT</c>. Заранее не создаётся:
    /// программа создаёт его сама.
    /// </summary>
    public string OutDir => Path.Combine(Root, "out");

    /// <summary>Каталог hex-файлов таблиц <c>&lt;out&gt;\pdata</c>.</summary>
    public string HexDir => Path.Combine(OutDir, "pdata");

    /// <summary>Книга со строками языков <c>&lt;out&gt;\PDATA.xlsx</c>.</summary>
    public string StringsWorkbook => Path.Combine(OutDir, "PDATA.xlsx");

    /// <summary>Книга с листами «Теги» и «Ошибки» <c>&lt;out&gt;\panel_data.xlsx</c>.</summary>
    public string PanelDataWorkbook => Path.Combine(OutDir, "panel_data.xlsx");

    /// <summary>
    /// Копирует файл из репозитория во временный каталог.
    /// </summary>
    /// <param name="relative">Путь файла относительно корня репозитория, части через <c>/</c>.</param>
    /// <param name="name">Имя копии во временном каталоге.</param>
    /// <returns>Абсолютный путь копии.</returns>
    public string CopyFromRepo(string relative, string name)
    {
        var target = Path.Combine(Root, name);
        File.Copy(RepositoryPaths.InRepo(relative), target);
        return target;
    }

    /// <summary>
    /// Распаковывает zip-архив из репозитория во временный каталог: папки архива оказываются
    /// прямо в <see cref="Root"/>.
    /// </summary>
    /// <param name="relative">Путь архива относительно корня репозитория, части через <c>/</c>.</param>
    /// <param name="folder">Папка верхнего уровня в архиве, путь к которой нужен тесту.</param>
    /// <returns>Абсолютный путь распакованной папки <paramref name="folder"/>.</returns>
    /// <exception cref="DirectoryNotFoundException">В архиве нет папки <paramref name="folder"/>.</exception>
    public string ExtractFromRepo(string relative, string folder)
    {
        System.IO.Compression.ZipFile.ExtractToDirectory(RepositoryPaths.InRepo(relative), Root);
        var path = Path.Combine(Root, folder);
        return Directory.Exists(path) ? path : throw new DirectoryNotFoundException($"в архиве {relative} нет папки {folder}");
    }

    /// <summary>
    /// Путь внутри временного каталога, по которому файла нет: данные ПЛК «неизвестно».
    /// </summary>
    /// <returns>Абсолютный путь несуществующего <c>PEData.plf</c>.</returns>
    public string MissingFile() => Path.Combine(Root, "нет-такого-файла", "PEData.plf");

    /// <summary>Удаляет временный каталог со всем содержимым; отсутствие каталога не ошибка.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
