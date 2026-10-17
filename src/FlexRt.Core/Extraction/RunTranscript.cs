using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Extraction;

/// <summary>
/// Текст запуска для пользователя: сводка панели, строка «Файл ПЛК:», предупреждения, ошибки и итоги экспортов —
/// по строке с уровнем (<see cref="TranscriptLine"/>) в порядке событий. Этот текст раньше печатала консоль: строки
/// <see cref="TranscriptLevel.Info"/> — в stdout, остальные — в stderr. Подробности для лога (время, стек, итог
/// сопоставления) сюда не попадают.
/// </summary>
public sealed class RunTranscript : IExtractionReporter
{
    /// <summary>Строки в порядке событий.</summary>
    private readonly List<TranscriptLine> _lines = [];

    /// <summary>Строки в порядке событий.</summary>
    public IReadOnlyList<TranscriptLine> Lines => _lines;

    /// <summary>Ошибка «Не удалось прочитать путь: сообщение».</summary>
    public void PanelReadFailed(string path, Exception error) => Error($"Не удалось прочитать {path}: {error.Message}");

    /// <summary>
    /// Сводка панели: файл, таблицы TOC, строки, теги, системные события, дискретные и аналоговые алармы, классы
    /// алармов (<see cref="AlarmClassesTable.Classes"/>), группы алармов (<see cref="AlarmGroupsTable.Groups"/>), число
    /// обычных экранов (<see cref="ScreensTable.ScreenCount"/>), стартовый экран (<see cref="StartScreenText.Format"/>),
    /// число картинок; затем предупреждения разбора.
    /// </summary>
    public void PanelRead(string path, FwxDocument doc)
    {
        Info($"Файл:    {path} ({doc.Binary.Length} байт)");
        Info($"Таблиц:  {doc.Toc.Count}");
        foreach (var t in doc.Toc)
        {
            Info($"  0x{t.Id,-4:x} {t.Name,-24} элементов: {t.Entries,-6} offset: 0x{t.Offset:x} size: {t.Size}");
        }
        Info($"Строк языков: {doc.Strings.Count}");
        Info($"Тегов: {doc.Tags.Count}");
        Info($"Системных событий: {doc.SystemEvents.Count}");
        Info($"Дискретных алармов: {doc.DiscreteAlarms.Count}");
        Info($"Аналоговых алармов: {doc.AnalogAlarms.Count}");
        Info($"Классов алармов: {AlarmClassesTable.Classes(doc).Count}");
        Info($"Групп алармов: {AlarmGroupsTable.Groups(doc).Count}");
        Info($"Экранов: {ScreensTable.ScreenCount(doc)}");
        Info($"Стартовый экран: {StartScreenText.Format(doc)}");
        Info($"Картинок: {doc.Pictures.Count}");
        foreach (var warning in doc.Warnings)
        {
            Warning($"ПРЕДУПРЕЖДЕНИЕ: не разобрано - {warning}");
        }
    }

    /// <summary>Ошибка «ОШИБКА: сообщение».</summary>
    public void Unsupported(string message) => Error($"ОШИБКА: {message}");

    /// <summary>Строка «Файл ПЛК: путь не найден - данные ПЛК: неизвестно».</summary>
    public void PlcNotFound(string path) => Info($"Файл ПЛК: {path} не найден - данные ПЛК: неизвестно");

    /// <summary>Строка «Файл ПЛК: путь (карта ПЛК; ПЛК: N, тегов ПЛК: M)»; «карта ПЛК; » — только у карты.</summary>
    public void PlcRead(string path, bool isCard, PlcProject project, long elapsedMs) =>
        Info($"Файл ПЛК: {path} ({(isCard ? "карта ПЛК; " : "")}ПЛК: {project.Devices.Count}, тегов ПЛК: {project.Tags.Count})");

    /// <summary>Строка «Файл ПЛК: путь не прочитан - данные ПЛК: неизвестно»; причина придёт в <see cref="PlcError"/>.</summary>
    public void PlcReadFailed(string path, Exception error) => Info($"Файл ПЛК: {path} не прочитан - данные ПЛК: неизвестно");

    /// <summary>Строки нет: причина придёт в <see cref="PlcError"/>, стек — только в лог.</summary>
    public void MatchFailed(Exception error)
    {
    }

    /// <summary>Строки нет: итог сопоставления — только в лог.</summary>
    public void Matched(IReadOnlyList<PlcMatch?> matches)
    {
    }

    /// <summary>Предупреждение «ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - сообщение».</summary>
    public void PlcError(string message) => Warning($"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {message}");

    /// <summary>Предупреждение «ПРЕДУПРЕЖДЕНИЕ: таблица сохранена целиком (raw.hex.txt) - сообщение».</summary>
    public void RawTableSaved(string warning) => Warning($"ПРЕДУПРЕЖДЕНИЕ: таблица сохранена целиком (raw.hex.txt) - {warning}");

    /// <summary>Успех — строка «[название] итог», ошибка — «[название] ОШИБКА: сообщение».</summary>
    public void ExportFinished(ExportOutcome outcome)
    {
        if (outcome.Error is not null)
        {
            Error($"[{outcome.Title}] ОШИБКА: {outcome.Error.Message}");
            return;
        }
        Info($"[{outcome.Title}] {outcome.Result}");
    }

    /// <summary>Добавить строку уровня <see cref="TranscriptLevel.Info"/>.</summary>
    private void Info(string text) => _lines.Add(new TranscriptLine(TranscriptLevel.Info, text));

    /// <summary>Добавить строку уровня <see cref="TranscriptLevel.Warning"/>.</summary>
    private void Warning(string text) => _lines.Add(new TranscriptLine(TranscriptLevel.Warning, text));

    /// <summary>Добавить строку уровня <see cref="TranscriptLevel.Error"/>.</summary>
    private void Error(string text) => _lines.Add(new TranscriptLine(TranscriptLevel.Error, text));
}
