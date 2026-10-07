namespace FlexRt.Tests.Integration;

/// <summary>
/// Результат одного запуска FlexRt: код возврата и захваченные потоки вывода.
/// </summary>
/// <param name="ExitCode">Код возврата процесса.</param>
/// <param name="StdOut">Весь stdout в UTF-8.</param>
/// <param name="StdErr">Весь stderr в UTF-8.</param>
public sealed record ProgramRun(int ExitCode, string StdOut, string StdErr);
