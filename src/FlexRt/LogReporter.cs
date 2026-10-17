using FlexRt.Export;
using FlexRt.Export.PanelData.Alarms;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Extraction;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using Serilog;

namespace FlexRt;

/// <summary>
/// События запуска в лог Serilog: сводка, предупреждения, итоги и ошибки со стеком. В консоль ничего не пишется
/// (решение пользователя: текст запуска показывает окно — <see cref="RunTranscript"/>).
/// </summary>
internal sealed class LogReporter : IExtractionReporter
{
    /// <summary>Ошибка чтения панели со стеком — в лог.</summary>
    public void PanelReadFailed(string path, Exception error) => Log.Error(error, "Не удалось прочитать панель {Panel}", path);

    /// <summary>
    /// В лог — размер файла, число таблиц, строк, тегов, системных событий, дискретных и аналоговых алармов, классов
    /// алармов (<see cref="AlarmClassesTable.Classes"/>), групп алармов (<see cref="AlarmGroupsTable.Groups"/>), обычных
    /// экранов (<see cref="ScreensTable.ScreenCount"/>), стартовый экран (<see cref="StartScreenText.Format"/>), число
    /// картинок, связей и соединений; по строке на соединение и на предупреждение разбора.
    /// </summary>
    public void PanelRead(string path, FwxDocument doc)
    {
        var alarmClasses = AlarmClassesTable.Classes(doc).Count;
        var alarmGroups = AlarmGroupsTable.Groups(doc).Count;
        var screens = ScreensTable.ScreenCount(doc);
        var startScreen = StartScreenText.Format(doc);
        Log.Information("Панель: {Bytes} байт, таблиц {Tables}, строк языков {Strings}, тегов {Tags}, системных событий {SystemEvents}, дискретных алармов {DiscreteAlarms}, аналоговых алармов {AnalogAlarms}, классов алармов {AlarmClasses}, групп алармов {AlarmGroups}, экранов {Screens}, стартовый экран {StartScreen}, картинок {Pictures}, связей с ПЛК {Links}, указателей областей {AreaLinks}, соединений {Connections}",
            doc.Binary.Length, doc.Toc.Count, doc.Strings.Count, doc.Tags.Count, doc.SystemEvents.Count, doc.DiscreteAlarms.Count, doc.AnalogAlarms.Count, alarmClasses, alarmGroups, screens, startScreen, doc.Pictures.Count, doc.Links.Count, doc.AreaLinks.Count, doc.Connections.Count);
        foreach (var connection in doc.Connections)
        {
            Log.Information("Соединение {Index}: {Name}, IP ПЛК {Ip}", connection.Index, connection.Name, IpAddressText.Format(connection.Ip));
        }
        foreach (var warning in doc.Warnings)
        {
            Log.Warning("Не разобрано: {Warning}", warning);
        }
    }

    /// <summary>«Запуск прекращён: сообщение» — в лог.</summary>
    public void Unsupported(string message) => Log.Error("Запуск прекращён: {Message}", message);

    /// <summary>«Файл ПЛК не найден» — в лог.</summary>
    public void PlcNotFound(string path) => Log.Information("Файл ПЛК {Plc} не найден: данные ПЛК неизвестны", path);

    /// <summary>В лог — время, число и имена ПЛК, числа тегов ПЛК, DB и неразобранных объектов.</summary>
    public void PlcRead(string path, bool isCard, PlcProject project, long elapsedMs) =>
        Log.Information("Проект ПЛК прочитан за {Elapsed} мс: ПЛК {Devices} ({Names}), тегов ПЛК {Tags}, DB {Dbs}, неразобранных объектов {Problems}",
            elapsedMs, project.Devices.Count, string.Join(", ", project.Devices.Select(d => d.Name ?? "?")),
            project.Tags.Count, project.Dbs.Count, project.Problems.Count);

    /// <summary>Ошибка чтения проекта ПЛК со стеком — в лог.</summary>
    public void PlcReadFailed(string path, Exception error) => Log.Error(error, "Файл ПЛК {Plc} не прочитан", path);

    /// <summary>Ошибка сопоставления со стеком — в лог.</summary>
    public void MatchFailed(Exception error) => Log.Error(error, "Ошибка сопоставления с ПЛК");

    /// <summary>Итог сопоставления по видам результата — в лог.</summary>
    public void Matched(IReadOnlyList<PlcMatch?> matches)
    {
        var byLookup = matches.Where(m => m is not null).GroupBy(m => m!.Lookup).Select(g => $"{g.Key}: {g.Count()}").ToList();
        Log.Information("Сопоставление: внутренних тегов {Internal}, PLC-тегов {Plc}{ByLookup}",
            matches.Count(m => m is null), matches.Count(m => m is not null), byLookup.Count == 0 ? "" : $" ({string.Join(", ", byLookup)})");
    }

    /// <summary>Проблема проекта ПЛК — предупреждение в лог.</summary>
    public void PlcError(string message) => Log.Warning("Файл ПЛК: {Error}", message);

    /// <summary>Таблица, сохранённая целиком, — предупреждение в лог.</summary>
    public void RawTableSaved(string warning) => Log.Warning("Таблица сохранена целиком (raw.hex.txt): {Warning}", warning);

    /// <summary>Итог экспорта в лог: успех — с временем, ошибка — со стеком.</summary>
    public void ExportFinished(ExportOutcome outcome)
    {
        if (outcome.Error is not null)
        {
            Log.Error(outcome.Error, "[{Title}] ошибка экспорта", outcome.Title);
            return;
        }
        Log.Information("[{Title}] {Result} ({Elapsed} мс)", outcome.Title, outcome.Result, outcome.ElapsedMs);
    }
}
