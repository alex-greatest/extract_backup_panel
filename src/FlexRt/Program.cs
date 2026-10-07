using System.Diagnostics;
using System.Text;
using FlexRt.Binary;
using FlexRt.Export.Hex;
using FlexRt.Export.Strings;
using FlexRt.Export.Tags;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Matching;
using FlexRt.Parsing.Panel;
using FlexRt.Parsing.Plc;
using Serilog;

// ---- статичные пути: поменяй под себя ----
// панель: pdata.fwc из папки Generates проекта TIA (подменяется FLEXRT_INPUT)
const string defaultFwxPath = @"C:\Users\Alexander\Desktop\extract_backup_data\Project1\IM\HMI\C\0\Generates\pdata.fwc";
// строки языков, лист на язык
const string xlsxPath = @"D:\projects\extract_backup\data\out\PDATA.xlsx";
// hex-файлы всех таблиц панели; папка удаляется перед записью
const string explodeDir = @"D:\projects\extract_backup\data\out\pdata";
// лист «Теги» (и «Ошибки»)
const string panelDataPath = @"D:\projects\extract_backup\data\out\panel_data.xlsx";
// проект ПЛК: System\PEData.plf проекта TIA (подменяется FLEXRT_PLC)
const string defaultPlcPath = @"C:\Users\Alexander\Desktop\extract_backup_data\Project1\System\PEData.plf";
// лог запуска: файл на день (flexrt-20261006.log), хранятся последние 14
const string logPath = @"D:\projects\extract_backup\data\out\logs\flexrt-.log";
// ------------------------------------------

// входной файл: из переменной окружения FLEXRT_INPUT, если она задана и не пуста, иначе defaultFwxPath
var inputOverride = Environment.GetEnvironmentVariable("FLEXRT_INPUT");
var fwxPath = string.IsNullOrWhiteSpace(inputOverride) ? defaultFwxPath : inputOverride;
// проект ПЛК: из переменной окружения FLEXRT_PLC, если она задана и не пуста, иначе defaultPlcPath
var plcOverride = Environment.GetEnvironmentVariable("FLEXRT_PLC");
var plcPath = string.IsNullOrWhiteSpace(plcOverride) ? defaultPlcPath : plcOverride;
// каталог результата: из переменной окружения FLEXRT_OUT, если она задана и не пуста, иначе data\out;
// внутри неё — фиксированные имена, не зависящие от констант выше
var outOverride = Environment.GetEnvironmentVariable("FLEXRT_OUT");
var stringsWorkbookPath = OutPath(xlsxPath, "PDATA.xlsx");
var hexDir = OutPath(explodeDir, "pdata");
var panelWorkbookPath = OutPath(panelDataPath, "panel_data.xlsx");
var logFilePath = OutPath(logPath, Path.Combine("logs", "flexrt-.log"));

Console.OutputEncoding = Encoding.UTF8;
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, encoding: Encoding.UTF8,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
var clock = Stopwatch.StartNew();
try
{
    var code = RunAll();
    Log.Information("Завершено за {Elapsed} мс, код возврата {Code}", clock.ElapsedMilliseconds, code);
    return code;
}
catch (Exception e)
{
    Log.Fatal(e, "Аварийное завершение");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Весь запуск: чтение панели, проекта ПЛК, сопоставление, три независимых экспорта.
// Возвращает код возврата: 0 — без предупреждений и ошибок, 1 — иначе.
int RunAll()
{
    Log.Information("Запуск FlexRt: панель {Panel} (FLEXRT_INPUT: {InputOverride}), проект ПЛК {Plc} (FLEXRT_PLC: {PlcOverride}), каталог результата {Out} (FLEXRT_OUT: {OutOverride})",
        fwxPath, inputOverride ?? "не задана", plcPath, plcOverride ?? "не задана",
        Path.GetDirectoryName(stringsWorkbookPath), outOverride ?? "не задана");

    FwxDocument doc;
    try
    {
        doc = FwxReader.Read(fwxPath);
    }
    catch (Exception e) when (e is FwxFormatException or IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Не удалось прочитать {fwxPath}: {e.Message}");
        Log.Error(e, "Не удалось прочитать панель {Panel}", fwxPath);
        return 1;
    }

    PrintSummary(doc);
    var failed = doc.Warnings.Count;

    var plcErrors = new List<string>();
    var plcProject = ReadPlcProject(plcPath, plcErrors);
    var matches = MatchTags(doc, plcProject, plcErrors);
    foreach (var error in plcErrors)
    {
        failed++;
        Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - {error}");
        Log.Warning("Файл ПЛК: {Error}", error);
    }

    failed += Run("Языки -> XLSX", () => WriteWorkbook(stringsWorkbookPath, doc.Strings.Count == 0, "в STRINGSTORE нет строк, пропуск",
        () => $"{XlsxExporter.Export(doc.Strings, stringsWorkbookPath)} языков, {doc.Strings.Count} строк -> {stringsWorkbookPath}"));

    failed += Run("Теги -> XLSX", () => WriteWorkbook(panelWorkbookPath, doc.Tags.Count == 0, "в VAR нет тегов, пропуск",
        () => $"{PanelDataExporter.Export(doc, matches, plcErrors, panelWorkbookPath)} тегов -> {panelWorkbookPath}"));

    var hexWarnings = new List<string>();
    failed += Run("Explode -> HEX", () =>
    {
        var files = HexExporter.Export(doc, hexDir, hexWarnings);
        foreach (var warning in hexWarnings)
        {
            Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: таблица сохранена целиком (raw.hex.txt) - {warning}");
            Log.Warning("Таблица сохранена целиком (raw.hex.txt): {Warning}", warning);
        }
        return $"{files} файлов -> {hexDir}";
    });
    failed += hexWarnings.Count;

    return failed == 0 ? 0 : 1;
}

// Путь выхода: без FLEXRT_OUT — путь по умолчанию (константа) как есть; с FLEXRT_OUT — фиксированное
// имя nameInOut внутри FLEXRT_OUT. Константы на результат с FLEXRT_OUT не влияют, поэтому ни один
// выход не уходит мимо FLEXRT_OUT, даже если константу перенести на другой диск.
string OutPath(string defaultPath, string nameInOut) => string.IsNullOrWhiteSpace(outOverride)
    ? defaultPath
    : Path.Combine(outOverride, nameInOut);

// Напечатать сводку панели: файл, таблицы TOC, строки, теги, предупреждения разбора; то же — в лог.
void PrintSummary(FwxDocument doc)
{
    Console.WriteLine($"Файл:    {fwxPath} ({doc.Binary.Length} байт)");
    Console.WriteLine($"Таблиц:  {doc.Toc.Count}");
    foreach (var t in doc.Toc)
    {
        Console.WriteLine($"  0x{t.Id,-4:x} {t.Name,-24} элементов: {t.Entries,-6} offset: 0x{t.Offset:x} size: {t.Size}");
    }
    Console.WriteLine($"Строк языков: {doc.Strings.Count}");
    Console.WriteLine($"Тегов: {doc.Tags.Count}");
    Log.Information("Панель: {Bytes} байт, таблиц {Tables}, строк языков {Strings}, тегов {Tags}, связей с ПЛК {Links}, указателей областей {AreaLinks}, соединений {Connections}",
        doc.Binary.Length, doc.Toc.Count, doc.Strings.Count, doc.Tags.Count, doc.Links.Count, doc.AreaLinks.Count, doc.Connections.Count);
    foreach (var connection in doc.Connections)
    {
        Log.Information("Соединение {Index}: {Name}, IP ПЛК {Ip}", connection.Index, connection.Name,
            $"{connection.Ip >> 24}.{(connection.Ip >> 16) & 0xff}.{(connection.Ip >> 8) & 0xff}.{connection.Ip & 0xff}");
    }
    foreach (var warning in doc.Warnings)
    {
        Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: не разобрано - {warning}");
        Log.Warning("Не разобрано: {Warning}", warning);
    }
}

// Выполнить экспорт, напечатать его итог и записать в лог с временем. Экспорты независимы:
// сбой одного не отменяет остальные. Возвращает 1 при ошибке, иначе 0.
int Run(string title, Func<string> action)
{
    var timer = Stopwatch.StartNew();
    try
    {
        var result = action();
        Console.WriteLine($"[{title}] {result}");
        Log.Information("[{Title}] {Result} ({Elapsed} мс)", title, result, timer.ElapsedMilliseconds);
        return 0;
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"[{title}] ОШИБКА: {e.Message}");
        Log.Error(e, "[{Title}] ошибка экспорта", title);
        return 1;
    }
}

// Прочитать проект ПЛК и напечатать строку сводки. Нет файла — строка «не найден», null,
// ошибкой не считается. Файл сломан или не открывается — сообщение в errors, null. Любая
// другая (непредвиденная) ошибка разбора — так же, с типом ошибки в сообщении: данные панели
// всё равно выгружаются; полный стек — в лог. Теги ПЛК, которые не удалось разобрать, —
// сообщения в errors, проект при этом возвращается.
PlcProject? ReadPlcProject(string path, List<string> errors)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл ПЛК: {path} не найден - данные ПЛК: неизвестно");
        Log.Information("Файл ПЛК {Plc} не найден: данные ПЛК неизвестны", path);
        return null;
    }
    var timer = Stopwatch.StartNew();
    try
    {
        var project = PlfReader.Read(path);
        Console.WriteLine($"Файл ПЛК: {path} (ПЛК: {project.Devices.Count}, тегов ПЛК: {project.Tags.Count})");
        Log.Information("Проект ПЛК прочитан за {Elapsed} мс: ПЛК {Devices} ({Names}), тегов ПЛК {Tags}, DB {Dbs}, неразобранных объектов {Problems}",
            timer.ElapsedMilliseconds, project.Devices.Count, string.Join(", ", project.Devices.Select(d => d.Name ?? "?")),
            project.Tags.Count, project.Dbs.Count, project.Problems.Count);
        errors.AddRange(project.Problems);
        return project;
    }
    catch (Exception e)
    {
        Console.WriteLine($"Файл ПЛК: {path} не прочитан - данные ПЛК: неизвестно");
        Log.Error(e, "Файл ПЛК {Plc} не прочитан", path);
        errors.Add($"{path}: {Describe(e)}");
        return null;
    }
}

// Сопоставить теги панели с проектом ПЛК и записать в лог итог по видам результата.
// Непредвиденная ошибка сопоставления — сообщение в errors, полный стек в лог и сопоставление
// без проекта ПЛК (колонки ПЛК «неизвестно»).
List<PlcMatch?> MatchTags(FwxDocument doc, PlcProject? project, List<string> errors)
{
    List<PlcMatch?> matches;
    try
    {
        matches = TagMatcher.Match(doc, project);
    }
    catch (Exception e)
    {
        Log.Error(e, "Ошибка сопоставления с ПЛК");
        errors.Add($"сопоставление с ПЛК: {Describe(e)}");
        matches = TagMatcher.Match(doc, null);
    }
    var byLookup = matches.Where(m => m is not null).GroupBy(m => m!.Lookup).Select(g => $"{g.Key}: {g.Count()}").ToList();
    Log.Information("Сопоставление: внутренних тегов {Internal}, PLC-тегов {Plc}{ByLookup}",
        matches.Count(m => m is null), matches.Count(m => m is not null), byLookup.Count == 0 ? "" : $" ({string.Join(", ", byLookup)})");
    return matches;
}

// Текст ошибки для сводки и листа «Ошибки»: ошибки формата и чтения — их сообщение,
// непредвиденная — «непредвиденная ошибка <тип>: <сообщение>».
string Describe(Exception e) => e is FwxFormatException or IOException or UnauthorizedAccessException
    ? e.Message
    : $"непредвиденная ошибка {e.GetType().Name}: {e.Message}";

// Записать книгу XLSX и вернуть итог экспорта. Нет данных — удалить файл прошлого запуска
// и вернуть сообщение о пропуске; иначе создать каталог файла и выполнить экспорт.
string WriteWorkbook(string path, bool isEmpty, string skipMessage, Func<string> export)
{
    if (isEmpty)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return skipMessage;
    }
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    return export();
}
