using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Шаговые копии <c>pdata.fwc</c>, сверенные с TIA, в <c>data/real/</c> (вне git): папки и путь к копии; копии нет — тест
/// падает.
/// </summary>
public static class StepCopies
{
    /// <summary>Внутренние теги (TIA V17): в каждой следующей копии добавлен один тег.</summary>
    public const string Tags = "data/real/steps";

    /// <summary>Свойства экранов и подсказки Project1 (TIA V21, TP700 Comfort, один язык).</summary>
    public const string ScreenProps = "data/real/steps-screen-props";

    /// <summary>Подсказка экрана A603A0097 (TIA V21, TP1900 Comfort) на трёх языках.</summary>
    public const string TooltipLangs = "data/real/steps-tooltip-langs";

    /// <summary>Папка шаговых копий; её нет — провал теста с сообщением, что копии вне git.</summary>
    /// <param name="folder">Папка копий относительно корня репозитория.</param>
    /// <returns>Абсолютный путь к папке.</returns>
    public static string Folder(string folder)
    {
        var path = RepositoryPaths.InRepo(folder);
        Assert.True(Directory.Exists(path), $"нет {folder} (шаговые копии вне git): проверка невозможна");
        return path;
    }

    /// <summary>Путь к шаговой копии; файла нет — провал теста с сообщением, что копии вне git.</summary>
    /// <param name="folder">Папка копий относительно корня репозитория.</param>
    /// <param name="step">Имя файла копии.</param>
    /// <returns>Абсолютный путь к копии.</returns>
    public static string Path(string folder, string step)
    {
        var path = RepositoryPaths.InRepo($"{folder}/{step}");
        Assert.True(File.Exists(path), $"нет {folder}/{step} (шаговые копии вне git): проверка невозможна");
        return path;
    }
}
