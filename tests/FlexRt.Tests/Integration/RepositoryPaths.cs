namespace FlexRt.Tests.Integration;

/// <summary>
/// Пути репозитория для интеграционных тестов: корень ищется вверх от папки сборки тестов
/// по файлу решения <c>FlexRt.sln</c>.
/// </summary>
public static class RepositoryPaths
{
    /// <summary>Имя файла решения, по которому опознаётся корень репозитория.</summary>
    private const string SolutionFileName = "FlexRt.sln";

    /// <summary>Корень репозитория — каталог, в котором лежит <c>FlexRt.sln</c>.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>
    /// Возвращает абсолютный путь к файлу или каталогу внутри репозитория.
    /// </summary>
    /// <param name="relative">Путь относительно корня, части через <c>/</c>.</param>
    /// <returns>Абсолютный путь.</returns>
    public static string InRepo(string relative) =>
        Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    /// Находит корень репозитория, поднимаясь от <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    /// <returns>Каталог, содержащий <c>FlexRt.sln</c>.</returns>
    /// <exception cref="InvalidOperationException">Файл решения не найден ни в одном родительском каталоге.</exception>
    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            {
                return dir.FullName;
            }
        }
        throw new InvalidOperationException($"Не найден {SolutionFileName} выше {AppContext.BaseDirectory}");
    }
}
