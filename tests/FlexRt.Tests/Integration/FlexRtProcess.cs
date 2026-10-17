using System.Diagnostics;
using System.Text;
using FlexRt.Extraction;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Запускает FlexRt отдельным процессом <c>dotnet FlexRt.dll</c> из корня репозитория
/// с подменой входных файлов через <c>FLEXRT_INPUT</c> и <c>FLEXRT_PLC</c> и каталога
/// результата через <c>FLEXRT_OUT</c>. Консоль ничего не печатает, поэтому текст запуска
/// берётся из того же запуска внутри процесса теста (<see cref="RunTranscript"/>).
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
    /// Запускает программу и ждёт её завершения; её stdout и stderr должны быть пусты. Перед этим тот же запуск
    /// (<see cref="ExtractionRun.Execute"/>) с теми же путями выполняется внутри процесса теста с
    /// <see cref="RunTranscript"/> — так пути в строках экспортов совпадают с путями процесса. Каталога результата до
    /// запуска быть не должно; после запуска внутри теста он удаляется, чтобы проверялись только файлы процесса. Код возврата запуска внутри теста
    /// должен совпасть с кодом процесса.
    /// </summary>
    /// <param name="input">Путь к <c>pdata.fwc</c> (значение <c>FLEXRT_INPUT</c>).</param>
    /// <param name="plc">Путь к <c>PEData.plf</c> (значение <c>FLEXRT_PLC</c>; файл может не существовать).</param>
    /// <param name="outDir">Каталог результата (значение <c>FLEXRT_OUT</c>).</param>
    /// <returns>Код возврата процесса и текст запуска.</returns>
    /// <exception cref="FileNotFoundException">Рядом с тестами нет <c>FlexRt.dll</c>.</exception>
    /// <exception cref="TimeoutException">Программа не завершилась за отведённое время; процесс убит.</exception>
    public static ProgramRun Run(string input, string plc, string outDir)
    {
        if (!File.Exists(ProgramDll))
        {
            throw new FileNotFoundException("Нет сборки программы рядом с тестами", ProgramDll);
        }
        Assert.False(Directory.Exists(outDir), $"каталог результата уже есть до запуска: {outDir}");
        var transcript = new RunTranscript();
        var expected = ExtractionRun.Execute(SamePaths(input, plc, outDir), transcript);
        if (Directory.Exists(outDir))
        {
            Directory.Delete(outDir, recursive: true);
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
        var output = stdOut.GetAwaiter().GetResult();
        var errors = stdErr.GetAwaiter().GetResult();
        Assert.True(output == "", $"программа печатает в stdout:\n{output}");
        Assert.True(errors == "", $"программа печатает в stderr:\n{errors}");
        Assert.True(process.ExitCode == expected.ExitCode, $"код возврата процесса {process.ExitCode}, внутри теста {expected.ExitCode}");
        AssertProcessLog(input, plc, outDir, process.ExitCode, transcript);
        return new ProgramRun(process.ExitCode, Text(transcript, l => l == TranscriptLevel.Info), Text(transcript, l => l != TranscriptLevel.Info));
    }

    /// <summary>
    /// Входы и выходы запуска внутри процесса теста — те же, что у программы с <c>FLEXRT_OUT</c>: фиксированные имена
    /// <c>PDATA.xlsx</c>, <c>panel_data.xlsx</c>, <c>pdata</c>, <c>images</c> в каталоге результата.
    /// </summary>
    /// <param name="input">Путь к <c>pdata.fwc</c>.</param>
    /// <param name="plc">Путь данных ПЛК.</param>
    /// <param name="outDir">Каталог результата.</param>
    /// <returns>Пути запуска.</returns>
    private static ExtractionPaths SamePaths(string input, string plc, string outDir) =>
        new(input, plc, Path.Combine(outDir, "PDATA.xlsx"), Path.Combine(outDir, "panel_data.xlsx"),
            Path.Combine(outDir, "pdata"), Path.Combine(outDir, "images"));

    /// <summary>
    /// Проверяет лог процесса <c>logs/flexrt-*.log</c> в каталоге результата: строка старта с переданными путями панели
    /// и данных ПЛК (так видно, что <c>FLEXRT_INPUT</c> и <c>FLEXRT_PLC</c> дошли до программы) и итоговая строка с
    /// кодом возврата процесса; для каждой строки текста запуска, у которой есть пара в логе
    /// (<see cref="ProcessLogLines.Expected"/>: предупреждения, ошибки, «Файл ПЛК:», итоги экспортов), — её запись.
    /// Других записей в каталоге нет: запуск внутри теста лог не пишет, а его каталог удалён.
    /// </summary>
    /// <param name="input">Путь к <c>pdata.fwc</c>, переданный процессу.</param>
    /// <param name="plc">Путь данных ПЛК, переданный процессу.</param>
    /// <param name="outDir">Каталог результата процесса.</param>
    /// <param name="exitCode">Код возврата процесса.</param>
    /// <param name="transcript">Текст того же запуска внутри процесса теста.</param>
    private static void AssertProcessLog(string input, string plc, string outDir, int exitCode, RunTranscript transcript)
    {
        var logDir = Path.Combine(outDir, "logs");
        var log = Directory.Exists(logDir)
            ? string.Join("\n", Directory.GetFiles(logDir, "flexrt-*.log").Order().Select(File.ReadAllText))
            : "";
        Assert.True(log.Contains($"Запуск FlexRt: панель {input} (FLEXRT_INPUT: {input}), проект ПЛК {plc} (FLEXRT_PLC: {plc})", StringComparison.Ordinal),
            $"в логе процесса нет строки старта с путями {input} и {plc}:\n{log}");
        Assert.True(log.Contains($"код возврата {exitCode}", StringComparison.Ordinal), $"в логе процесса нет кода возврата {exitCode}:\n{log}");
        foreach (var expected in transcript.Lines.Select(ProcessLogLines.Expected).OfType<string>())
        {
            Assert.True(log.Contains(expected, StringComparison.Ordinal), $"в логе процесса нет «{expected}»:\n{log}");
        }
    }

    /// <summary>Строки текста запуска выбранных уровней через перевод строки.</summary>
    /// <param name="transcript">Текст запуска.</param>
    /// <param name="level">Отбор уровня.</param>
    /// <returns>Строки через <c>\n</c>; нет строк — пусто.</returns>
    private static string Text(RunTranscript transcript, Func<TranscriptLevel, bool> level) =>
        string.Join("\n", transcript.Lines.Where(l => level(l.Level)).Select(l => l.Text));

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
