using Xunit.Abstractions;
using Xunit.Sdk;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Прогон сборки тестов, как у xUnit, с одним добавлением: когда все тесты сборки отработали, до сообщения о конце
/// прогона, удаляются каталоги общих запусков (<see cref="SharedRuns.DeleteAll"/>) — после прогона во временной папке
/// ничего не остаётся.
/// </summary>
public sealed class FlexRtTestAssemblyRunner : XunitTestAssemblyRunner
{
    /// <summary>Создаёт прогон сборки.</summary>
    /// <param name="testAssembly">Сборка тестов.</param>
    /// <param name="testCases">Тесты к прогону.</param>
    /// <param name="diagnosticMessageSink">Получатель диагностических сообщений xUnit.</param>
    /// <param name="executionMessageSink">Получатель сообщений о ходе прогона.</param>
    /// <param name="executionOptions">Параметры прогона.</param>
    public FlexRtTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink,
        IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
    {
    }

    /// <summary>Удалить каталоги общих запусков, затем закончить прогон как обычно.</summary>
    /// <returns>Задача окончания прогона.</returns>
    protected override Task BeforeTestAssemblyFinishedAsync()
    {
        SharedRuns.DeleteAll();
        return base.BeforeTestAssemblyFinishedAsync();
    }
}
