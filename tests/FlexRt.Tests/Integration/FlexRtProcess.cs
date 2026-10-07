using System.Diagnostics;
using System.Text;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Запускает FlexRt отдельным процессом <c>dotnet FlexRt.dll</c> из корня репозитория
/// с подменой входных файлов через <c>FLEXRT_INPUT</c> и <c>FLEXRT_PLC</c> и каталога
/// результата через <c>FLEXRT_OUT</c>.
/// </summary>
public static class FlexRtProcess
{
    /// <summary>
    /// Переменная окружения с полным путём к хосту <c>dotnet</c>, которым запущены сборка и
    /// тесты; её выставляют <c>dotnet test</c> и MSBuild.
    /// </summary>
    private const string DotnetHostVariable = "DOTNET_HOST_PATH";

    /// <summary>Сколько ждать завершения программы, прежде чем считать запуск зависшим.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Путь к <c>FlexRt.dll</c> в выходной папке тестов: ссылка на проект копирует туда
    /// сборку программы вместе с <c>FlexRt.runtimeconfig.json</c> и <c>FlexRt.deps.json</c>.
    /// </summary>
    private static string ProgramDll => Path.Combine(AppContext.BaseDirectory, "FlexRt.dll");

    /// <summary>
    /// Запускает программу и ждёт её завершения.
    /// </summary>
    /// <param name="input">Путь к <c>pdata.fwc</c> (значение <c>FLEXRT_INPUT</c>).</param>
    /// <param name="plc">Путь к <c>PEData.plf</c> (значение <c>FLEXRT_PLC</c>; файл может не существовать).</param>
    /// <param name="outDir">Каталог результата (значение <c>FLEXRT_OUT</c>).</param>
    /// <returns>Код возврата, stdout и stderr.</returns>
    /// <exception cref="FileNotFoundException">Рядом с тестами нет <c>FlexRt.dll</c>.</exception>
    /// <exception cref="TimeoutException">Программа не завершилась за отведённое время; процесс убит.</exception>
    public static ProgramRun Run(string input, string plc, string outDir)
    {
        if (!File.Exists(ProgramDll))
        {
            throw new FileNotFoundException("Нет сборки программы рядом с тестами", ProgramDll);
        }
        using var process = new Process();
        process.StartInfo = CreateStartInfo(input, plc, outDir);
        process.Start();
        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"FlexRt не завершилась за {Timeout.TotalSeconds} с");
        }
        process.WaitForExit();
        return new ProgramRun(process.ExitCode, stdOut.GetAwaiter().GetResult(), stdErr.GetAwaiter().GetResult());
    }

    /// <summary>
    /// Собирает параметры запуска: хост <c>dotnet</c> (<see cref="DotnetHost"/>) с путём к
    /// сборке, рабочая папка — корень репозитория, потоки перенаправлены в UTF-8, заданы
    /// переменные окружения входов и каталога результата.
    /// </summary>
    /// <param name="input">Значение <c>FLEXRT_INPUT</c>.</param>
    /// <param name="plc">Значение <c>FLEXRT_PLC</c>.</param>
    /// <param name="outDir">Значение <c>FLEXRT_OUT</c>.</param>
    /// <returns>Параметры процесса.</returns>
    private static ProcessStartInfo CreateStartInfo(string input, string plc, string outDir)
    {
        var info = new ProcessStartInfo(DotnetHost())
        {
            WorkingDirectory = RepositoryPaths.Root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        info.ArgumentList.Add(ProgramDll);
        info.Environment["FLEXRT_INPUT"] = input;
        info.Environment["FLEXRT_PLC"] = plc;
        info.Environment["FLEXRT_OUT"] = outDir;
        return info;
    }

    /// <summary>
    /// Хост для запуска программы: путь из <c>DOTNET_HOST_PATH</c>, если переменная задана и
    /// не пуста, — тот же <c>dotnet</c>, которым запущены тесты; иначе <c>dotnet</c> из PATH.
    /// </summary>
    /// <returns>Путь или имя исполняемого файла <c>dotnet</c>.</returns>
    private static string DotnetHost() =>
        Environment.GetEnvironmentVariable(DotnetHostVariable) is { Length: > 0 } host ? host : "dotnet";
}
