using FlexRt.Extraction;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Tests.Extraction;

/// <summary>
/// Подставной получатель событий <see cref="ExtractionRun"/>: записывает имя каждого события
/// по порядку, у экспорта — с названием и итогом (<c>ExportFinished Теги -> XLSX: ok</c> или
/// <c>: ошибка</c>). Может бросить исключение при сообщении об успехе выбранного экспорта —
/// так проверяется сбой печати итога.
/// </summary>
public sealed class RecordingReporter : IExtractionReporter
{
    /// <summary>Название экспорта, сообщение об успехе которого бросает <see cref="IOException"/>; <c>null</c> — не бросать.</summary>
    private readonly string? _failOnSuccessOf;

    /// <summary>Создаёт получатель.</summary>
    /// <param name="failOnSuccessOf">Название экспорта, сообщение об успехе которого бросает <see cref="IOException"/>; <c>null</c> — не бросать.</param>
    public RecordingReporter(string? failOnSuccessOf = null)
    {
        _failOnSuccessOf = failOnSuccessOf;
    }

    /// <summary>События по порядку.</summary>
    public List<string> Events { get; } = [];

    /// <summary>Сообщение последнего события <see cref="Unsupported"/>; <c>null</c>, если его не было.</summary>
    public string? UnsupportedMessage { get; private set; }

    /// <summary>Записывает <c>PanelReadFailed</c>.</summary>
    public void PanelReadFailed(string path, Exception error) => Events.Add("PanelReadFailed");

    /// <summary>Записывает <c>PanelRead</c>.</summary>
    public void PanelRead(string path, FwxDocument doc) => Events.Add("PanelRead");

    /// <summary>Записывает <c>Unsupported</c> и запоминает сообщение.</summary>
    public void Unsupported(string message)
    {
        Events.Add("Unsupported");
        UnsupportedMessage = message;
    }

    /// <summary>Записывает <c>PlcNotFound</c>.</summary>
    public void PlcNotFound(string path) => Events.Add("PlcNotFound");

    /// <summary>Записывает <c>PlcRead</c> с признаком карты: <c>PlcRead plf</c> или <c>PlcRead card</c>.</summary>
    public void PlcRead(string path, bool isCard, PlcProject project, long elapsedMs) => Events.Add(isCard ? "PlcRead card" : "PlcRead plf");

    /// <summary>Записывает <c>PlcReadFailed</c>.</summary>
    public void PlcReadFailed(string path, Exception error) => Events.Add("PlcReadFailed");

    /// <summary>Записывает <c>MatchFailed</c>.</summary>
    public void MatchFailed(Exception error) => Events.Add("MatchFailed");

    /// <summary>Записывает <c>Matched</c>.</summary>
    public void Matched(IReadOnlyList<PlcMatch?> matches) => Events.Add("Matched");

    /// <summary>Записывает <c>PlcError</c>.</summary>
    public void PlcError(string message) => Events.Add("PlcError");

    /// <summary>Записывает <c>RawTableSaved</c>.</summary>
    public void RawTableSaved(string warning) => Events.Add("RawTableSaved");

    /// <summary>
    /// Записывает <c>ExportFinished &lt;название&gt;: ok</c> или <c>: ошибка</c>. Успех экспорта
    /// <see cref="_failOnSuccessOf"/> не записывается, а бросает исключение.
    /// </summary>
    /// <exception cref="IOException">Успех экспорта <see cref="_failOnSuccessOf"/>.</exception>
    public void ExportFinished(ExportOutcome outcome)
    {
        if (outcome.Error is null && outcome.Title == _failOnSuccessOf)
        {
            throw new IOException("stdout не пишется");
        }
        Events.Add($"ExportFinished {outcome.Title}: {(outcome.Error is null ? "ok" : "ошибка")}");
    }
}
