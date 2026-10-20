using FlexRt.Extraction;
using FlexRt.Model.Matching;
using FlexRt.Parsing.Panel;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Extraction;

/// <summary>
/// Шаг данных ПЛК, общий у консоли и окна (<see cref="PlcDataReader.Read"/>), с подставным <see cref="RecordingReporter"/>:
/// сбой сопоставления. Панель — <c>samples/plc/pdata-one-connection.fwc</c>, прочитанная с диска; в памяти у её связей
/// номер соединения −1, на котором выбор ПЛК по соединению падает (файлов с такой связью нет и не создаётся).
/// </summary>
public sealed class PlcDataReaderTests
{
    /// <summary>
    /// Сопоставление с проектом ПЛК упало: <c>MatchFailed</c> после чтения проекта, затем <c>Matched</c> — сопоставление
    /// без проекта ПЛК, затем <c>PlcError</c> «сопоставление с ПЛК: …»; у PLC-тегов результат «неизвестно».
    /// </summary>
    [Fact]
    public void MatchingFails_ReportsMatchFailedThenFallback()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/plc/pdata-one-connection.fwc"));
        for (var i = 0; i < doc.Links.Count; i++)
        {
            doc.Links[i] = doc.Links[i] with { Connection = -1 };
        }
        var reporter = new RecordingReporter();

        var plc = PlcDataReader.Read(doc, RepositoryPaths.InRepo("samples/plc/PEData.plf"), reporter, "файлы не созданы");

        Assert.Equal(["PlcRead plf", "MatchFailed", "Matched", "PlcError"], reporter.Events);
        Assert.Single(plc.Errors, e => e.StartsWith("сопоставление с ПЛК: ", StringComparison.Ordinal));
        Assert.All(plc.Matches.OfType<PlcMatch>(), m => Assert.Equal(PlcLookup.Unknown, m.Lookup));
    }
}
