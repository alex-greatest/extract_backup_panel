using System.Collections.Concurrent;
using System.Diagnostics;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Запуски FlexRt на неизменных образцах без данных ПЛК, общие для тестов: каждый образец запускается один раз за
/// прогон (<see cref="FlexRtProcess.Run"/> со всеми его проверками), тесты только читают результат. Запуск лениво
/// создаётся первым тестом, которому он нужен; параллельные тесты ждут тот же запуск. Каталог запуска —
/// <c>flexrt-shared-&lt;PID&gt;-&lt;GUID&gt;</c> во временной папке; все они удаляются в конце прогона сборки
/// (<see cref="FlexRtTestAssemblyRunner"/>). Если прогон прерван (хост тестов убит), его каталоги удаляет следующий прогон —
/// по PID процесса, которого уже нет. Тесты, которые портят вход или задают данные ПЛК, запускают программу сами в своём
/// каталоге.
/// </summary>
public static class SharedRuns
{
    /// <summary>Начало имени каталогов общих запусков, до PID процесса тестов.</summary>
    private const string Prefix = "flexrt-shared-";

    /// <summary>Запуски по пути образца.</summary>
    private static readonly ConcurrentDictionary<string, Lazy<SharedRun>> Runs = new(StringComparer.Ordinal);

    /// <summary>Удалить каталоги прерванных прогонов, прежде чем создавать свои.</summary>
    static SharedRuns() => DeleteStale();

    /// <summary>Запуск на образце без данных ПЛК (путь ПЛК — несуществующий файл во временном каталоге).</summary>
    /// <param name="sample">Образец <c>pdata.fwc</c> относительно корня репозитория.</param>
    /// <returns>Каталог результата и запуск; один и тот же для всех тестов прогона.</returns>
    public static SharedRun WithoutPlc(string sample) => Runs.GetOrAdd(sample, s => new Lazy<SharedRun>(() => Start(s))).Value;

    /// <summary>Запустить программу на образце в новом временном каталоге.</summary>
    /// <returns>Каталог и запуск.</returns>
    private static SharedRun Start(string sample)
    {
        var dir = new TestDirectory($"{Prefix}{Environment.ProcessId}-");
        return new SharedRun(dir, FlexRtProcess.Run(RepositoryPaths.InRepo(sample), dir.MissingFile(), dir.OutDir));
    }

    /// <summary>
    /// Удалить каталоги общих запусков процессов тестов, которых уже нет (прогон прерван до своей очистки); каталоги
    /// живых процессов — параллельного прогона — не трогаются. Сбой удаления не мешает тестам.
    /// </summary>
    private static void DeleteStale()
    {
        foreach (var path in Directory.EnumerateDirectories(Path.GetTempPath(), $"{Prefix}*"))
        {
            var name = Path.GetFileName(path)[Prefix.Length..];
            var dash = name.IndexOf('-');
            if (dash <= 0 || !int.TryParse(name[..dash], out var pid) || IsRunning(pid))
            {
                continue;
            }
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // каталог занят — удалит следующий прогон
            }
        }
    }

    /// <summary>Жив ли процесс с номером <paramref name="pid"/>.</summary>
    /// <returns><c>true</c>, если процесс есть.</returns>
    private static bool IsRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // процесс есть, но доступа к нему нет — считается живым
            return true;
        }
    }

    /// <summary>
    /// Удалить каталоги всех выполненных запусков — в конце прогона сборки, когда тесты их больше не читают; сбой удаления
    /// не мешает завершению (каталог тогда удалит следующий прогон).
    /// </summary>
    public static void DeleteAll()
    {
        foreach (var run in Runs.Values.Where(r => r.IsValueCreated))
        {
            try
            {
                run.Value.Dir.Dispose();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // каталог занят другим процессом — удалит следующий прогон
            }
        }
    }
}
