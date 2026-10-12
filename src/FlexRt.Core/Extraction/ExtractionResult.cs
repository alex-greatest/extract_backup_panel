using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;

namespace FlexRt.Extraction;

/// <summary>Итог запуска извлечения: код возврата и всё прочитанное.</summary>
/// <param name="ExitCode">0 — без предупреждений и ошибок, 1 — иначе.</param>
/// <param name="Document">Прочитанная панель; <c>null</c>, если <c>pdata.fwc</c> не прочитан.</param>
/// <param name="PlcProject">Проект ПЛК; <c>null</c>, если не найден, не прочитан или до него не дошло.</param>
/// <param name="Matches">Сопоставление тегов панели с ПЛК по порядку тегов; пусто, если до него не дошло.</param>
/// <param name="PlcErrors">Сообщения о проблемах проекта ПЛК (лист «Ошибки»).</param>
/// <param name="Summary">Сведения для листа «Сводка»; <c>null</c>, если до них не дошло.</param>
/// <param name="Exports">Итоги экспортов в порядке выполнения; пусто, если до них не дошло.</param>
public sealed record ExtractionResult(
    int ExitCode,
    FwxDocument? Document,
    PlcProject? PlcProject,
    IReadOnlyList<PlcMatch?> Matches,
    IReadOnlyList<string> PlcErrors,
    RunSummary? Summary,
    IReadOnlyList<ExportOutcome> Exports);
