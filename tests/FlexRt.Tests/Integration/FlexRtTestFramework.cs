using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("FlexRt.Tests.Integration.FlexRtTestFramework", "FlexRt.Tests")]

namespace FlexRt.Tests.Integration;

/// <summary>
/// Тестовый фреймворк сборки: обычный xUnit, но прогон сборки идёт через <see cref="FlexRtTestAssemblyRunner"/>, который
/// по окончании всех тестов удаляет каталоги общих запусков (<see cref="SharedRuns"/>).
/// </summary>
public sealed class FlexRtTestFramework : XunitTestFramework
{
    /// <summary>Создаёт фреймворк; вызывает xUnit по атрибуту сборки.</summary>
    /// <param name="messageSink">Получатель диагностических сообщений xUnit.</param>
    public FlexRtTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
    }

    /// <summary>Исполнитель тестов сборки с очисткой в конце прогона.</summary>
    /// <param name="assemblyName">Сборка тестов.</param>
    /// <returns>Исполнитель.</returns>
    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new FlexRtTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}
