namespace FlexRt.Tests.Integration;

/// <summary>
/// Результат одного запуска FlexRt: код возврата процесса и текст запуска, который раньше печатала консоль, — строки
/// <see cref="FlexRt.Extraction.RunTranscript"/> того же запуска внутри процесса теста.
/// </summary>
/// <param name="ExitCode">Код возврата процесса.</param>
/// <param name="Output">Строки уровня Info (раньше stdout консоли), по строке на строку текста.</param>
/// <param name="Errors">Строки уровней Warning и Error (раньше stderr консоли), по строке на строку текста.</param>
public sealed record ProgramRun(int ExitCode, string Output, string Errors);
