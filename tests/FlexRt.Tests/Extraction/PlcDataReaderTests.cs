using FlexRt.Extraction;
using FlexRt.Model.Matching;
using FlexRt.Parsing.Panel;
using FlexRt.Tests.Integration;
using Xunit;

namespace FlexRt.Tests.Extraction;

/// <summary>
/// Шаг данных ПЛК окна (<see cref="PlcDataReader.Read"/>) с текстом запуска <see cref="RunTranscript"/>: сбой
/// сопоставления. Панель — <c>samples/plc/pdata-one-connection.fwc</c>, прочитанная с диска; в памяти у её связей
/// номер соединения −1, на котором выбор ПЛК по соединению падает (файлов с такой связью нет и не создаётся).
/// </summary>
public sealed class PlcDataReaderTests
{
    /// <summary>
    /// Сопоставление с проектом ПЛК упало: после строки «Файл ПЛК: …» — предупреждение «ПРЕДУПРЕЖДЕНИЕ: файл ПЛК -
    /// сопоставление с ПЛК: …», сопоставление повторено без проекта ПЛК — у PLC-тегов результат «неизвестно».
    /// </summary>
    [Fact]
    public void MatchingFails_ReportsErrorThenFallback()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/plc/pdata-one-connection.fwc"));
        for (var i = 0; i < doc.Links.Count; i++)
        {
            doc.Links[i] = doc.Links[i] with { Connection = -1 };
        }
        var transcript = new RunTranscript();
        var plcPath = RepositoryPaths.InRepo("samples/plc/PEData.plf");

        var plc = PlcDataReader.Read(doc, plcPath, transcript);

        Assert.Equal(2, transcript.Lines.Count);
        Assert.Equal(new TranscriptLine(TranscriptLevel.Info, $"Файл ПЛК: {plcPath} (ПЛК: 2, тегов ПЛК: 51)"), transcript.Lines[0]);
        Assert.Equal(TranscriptLevel.Warning, transcript.Lines[1].Level);
        Assert.StartsWith("ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - сопоставление с ПЛК: ", transcript.Lines[1].Text, StringComparison.Ordinal);
        Assert.Single(plc.Errors, e => e.StartsWith("сопоставление с ПЛК: ", StringComparison.Ordinal));
        Assert.All(plc.Matches.OfType<PlcMatch>(), m => Assert.Equal(PlcLookup.Unknown, m.Lookup));
    }
}
