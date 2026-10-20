using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FlexRt.Tests.Integration;

/// <summary>Исполнитель тестов сборки: как у xUnit, но сборку прогоняет <see cref="FlexRtTestAssemblyRunner"/>.</summary>
public sealed class FlexRtTestFrameworkExecutor : XunitTestFrameworkExecutor
{
    /// <summary>Создаёт исполнитель для сборки тестов.</summary>
    /// <param name="assemblyName">Сборка тестов.</param>
    /// <param name="sourceInformationProvider">Источник сведений о файлах и строках тестов.</param>
    /// <param name="diagnosticMessageSink">Получатель диагностических сообщений xUnit.</param>
    public FlexRtTestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
        : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
    }

    /// <summary>
    /// Прогнать тесты сборки через <see cref="FlexRtTestAssemblyRunner"/>. xUnit объявляет метод без результата и ждёт конца
    /// прогона по сообщениям, поэтому прогон запускается задачей <see cref="RunAsync"/>, а её исключения уходят в xUnit
    /// сообщением об ошибке, не роняя хост тестов.
    /// </summary>
    /// <param name="testCases">Тесты к прогону.</param>
    /// <param name="executionMessageSink">Получатель сообщений о ходе прогона.</param>
    /// <param name="executionOptions">Параметры прогона.</param>
    protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions) =>
        _ = RunAsync([.. testCases], executionMessageSink, executionOptions);

    /// <summary>Прогнать тесты сборки; исключение прогона — сообщение об ошибке xUnit.</summary>
    /// <param name="testCases">Тесты к прогону.</param>
    /// <param name="executionMessageSink">Получатель сообщений о ходе прогона.</param>
    /// <param name="executionOptions">Параметры прогона.</param>
    /// <returns>Задача прогона.</returns>
    private async Task RunAsync(List<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
    {
        try
        {
            using var runner = new FlexRtTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions);
            await runner.RunAsync();
        }
        catch (Exception e)
        {
            executionMessageSink.OnMessage(new ErrorMessage(testCases, e));
        }
    }
}
