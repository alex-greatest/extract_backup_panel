namespace FlexRt.Tests.Integration;

/// <summary>Общий запуск FlexRt на неизменном образце (<see cref="SharedRuns"/>): каталог с результатом и сам запуск.</summary>
/// <param name="Dir">Временный каталог запуска; результат — в <see cref="TestDirectory.OutDir"/>. Только для чтения.</param>
/// <param name="Run">Код возврата и текст запуска.</param>
public sealed record SharedRun(TestDirectory Dir, ProgramRun Run);
