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

    /// <summary>Анимация Visibility экрана <c>Screen_2</c> Project1 (TIA V21, TP700 Comfort): по одной настройке на шаг.</summary>
    public const string Animations = "data/real/steps-animations";

    /// <summary>
    /// События экранов Loaded и Cleared Project1 (TIA V21, TP700 Comfort): 09–13 — одна правка на шаг, 20–23 — все
    /// системные функции экрана с картой функций из TIA (<c>*.json</c>: экран, функция, параметры, таблица, байты),
    /// 24 — SetTag на член DB.
    /// </summary>
    public const string Events = "data/real/steps-events";

    /// <summary>
    /// Положение объектов Project1 (TIA V21, TP700 Comfort): 01–03 — Trend view на Screen_2, Screen_1 и Root screen,
    /// 04 — f(x) trend view на Root screen; значения Layout — со снимков TIA.
    /// </summary>
    public const string Objects = "data/real/steps-objects";

    /// <summary>
    /// Свойства I/O field <c>I/O field_1</c> на Screen_2 Project1 (TIA V21, TP700 Comfort): 00–32 и 34–36 — одно свойство
    /// на шаг через Openness, рядом с копией — XML экрана из TIA (<c>*.xml</c>), значения свойств.
    /// </summary>
    public const string IoField = "data/real/steps-iofield";

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
