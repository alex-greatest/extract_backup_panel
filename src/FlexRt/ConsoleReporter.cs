using FlexRt.Export;
using FlexRt.Extraction;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using Serilog;

namespace FlexRt;

/// <summary>
/// События запуска в консоль и в лог Serilog: сводка и итоги — в stdout, предупреждения и
/// ошибки — в stderr; в лог — то же с подробностями и стеком ошибок.
/// </summary>
internal sealed class ConsoleReporter : IExtractionReporter
{
    /// <summary>«Не удалось прочитать путь: сообщение» в stderr, ошибка со стеком — в лог.</summary>
    public void PanelReadFailed(string path, Exception error)
    {
        Console.Error.WriteLine($"Не удалось прочитать {path}: {error.Message}");
        Log.Error(error, "Не удалось прочитать панель {Panel}", path);
    }

    /// <summary>
    /// Сводка панели: файл, таблицы TOC, строки, теги, системные события, дискретные алармы; в
    /// лог — те же числа и соединения; предупреждения разбора — в stderr и в лог.
    /// </summary>
    public void PanelRead(string path, FwxDocument doc)
    {
        Console.WriteLine($"Файл:    {path} ({doc.Binary.Length} байт)");
        Console.WriteLine($"Таблиц:  {doc.Toc.Count}");
        foreach (var t in doc.Toc)
        {
            Console.WriteLine($"  0x{t.Id,-4:x} {t.Name,-24} элементов: {t.Entries,-6} offset: 0x{t.Offset:x} size: {t.Size}");
        }
        Console.WriteLine($"Строк языков: {doc.Strings.Count}");
        Console.WriteLine($"Тегов: {doc.Tags.Count}");
        Console.WriteLine($"Системных событий: {doc.SystemEvents.Count}");
        Console.WriteLine($"Дискретных алармов: {doc.DiscreteAlarms.Count}");
        Log.Information("Панель: {Bytes} байт, таблиц {Tables}, строк языков {Strings}, тегов {Tags}, системных событий {SystemEvents}, дискретных алармов {DiscreteAlarms}, связей с ПЛК {Links}, указателей областей {AreaLinks}, соединений {Connections}",
            doc.Binary.Length, doc.Toc.Count, doc.Strings.Count, doc.Tags.Count, doc.SystemEvents.Count, doc.DiscreteAlarms.Count, doc.Links.Count, doc.AreaLinks.Count, doc.Connections.Count);
        foreach (var connection in doc.Connections)
        {
            Log.Information("Соединение {Index}: {Name}, IP ПЛК {Ip}", connection.Index, connection.Name, IpAddressText.Format(connection.Ip));
        }
        foreach (var warning in doc.Warnings)
        {
            Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: не разобрано - {warning}");
            Log.Warning("Не разобрано: {Warning}", warning);
        }
    }

    /// <summary>«ОШИБКА: сообщение» в stderr и в лог.</summary>
    public void Unsupported(string message)
    {
        Console.Error.WriteLine($"ОШИБКА: {message}");
        Log.Error("Запуск прекращён: {Message}", message);
    }

    /// <summary>Строка «Файл ПЛК: путь не найден» в stdout и в лог.</summary>
    public void PlcNotFound(string path)
    {
        Console.WriteLine($"Файл ПЛК: {path} не найден - данные ПЛК: неизвестно");
        Log.Information("Файл ПЛК {Plc} не найден: данные ПЛК неизвестны", path);
    }

    /// <summary>Строка «Файл ПЛК: путь (ПЛК: N, тегов ПЛК: M)» в stdout; в лог — время, имена ПЛК, числа DB и неразобранных объектов.</summary>
    public void PlcRead(string path, bool isCard, PlcProject project, long elapsedMs)
    {
        Console.WriteLine($"Файл ПЛК: {path} ({(isCard ? "карта ПЛК; " : "")}ПЛК: {project.Devices.Count}, тегов ПЛК: {project.Tags.Count})");
        Log.Information("Проект ПЛК прочитан за {Elapsed} мс: ПЛК {Devices} ({Names}), тегов ПЛК {Tags}, DB {Dbs}, неразобранных объектов {Problems}",
            elapsedMs, project.Devices.Count, string.Join(", ", project.Devices.Select(d => d.Name ?? "?")),
            project.Tags.Count, project.Dbs.Count, project.Problems.Count);
    }

    /// <summary>Строка «Файл ПЛК: путь не прочитан» в stdout, ошибка со стеком — в лог.</summary>
    public void PlcReadFailed(string path, Exception error)
    {
        Console.WriteLine($"Файл ПЛК: {path} не прочитан - данные ПЛК: неизвестно");
        Log.Error(error, "Файл ПЛК {Plc} не прочитан", path);
    }

    /// <summary>Ошибка со стеком — в лог.</summary>
    public void MatchFailed(Exception error) => Log.Error(error, "Ошибка сопоставления с ПЛК");

    /// <summary>Итог сопоставления по видам результата — в лог.</summary>
    public void Matched(IReadOnlyList<PlcMatch?> matches)
    {
        var byLookup = matches.Where(m => m is not null).GroupBy(m => m!.Lookup).Select(g => $"{g.Key}: {g.Count()}").ToList();
        Log.Information("Сопоставление: внутренних тегов {Internal}, PLC-тегов {Plc}{ByLookup}",
            matches.Count(m => m is null), matches.Count(m => m is not null), byLookup.Count == 0 ? "" : $" ({string.Join(", ", byLookup)})");
    }

    /// <summary>«ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - сообщение» в stderr и в лог.</summary>
    public void PlcError(string message)
    {
        Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {message}");
        Log.Warning("Файл ПЛК: {Error}", message);
    }

    /// <summary>«ПРЕДУПРЕЖДЕНИЕ: таблица сохранена целиком» в stderr и в лог.</summary>
    public void RawTableSaved(string warning)
    {
        Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: таблица сохранена целиком (raw.hex.txt) - {warning}");
        Log.Warning("Таблица сохранена целиком (raw.hex.txt): {Warning}", warning);
    }

    /// <summary>Успех — «[название] итог» в stdout, ошибка — «[название] ОШИБКА: сообщение» в stderr; в лог — с временем или стеком.</summary>
    public void ExportFinished(ExportOutcome outcome)
    {
        if (outcome.Error is not null)
        {
            Console.Error.WriteLine($"[{outcome.Title}] ОШИБКА: {outcome.Error.Message}");
            Log.Error(outcome.Error, "[{Title}] ошибка экспорта", outcome.Title);
            return;
        }
        Console.WriteLine($"[{outcome.Title}] {outcome.Result}");
        Log.Information("[{Title}] {Result} ({Elapsed} мс)", outcome.Title, outcome.Result, outcome.ElapsedMs);
    }
}
