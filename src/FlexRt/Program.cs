using System.Diagnostics;
using System.Text;
using FlexRt;
using FlexRt.Extraction;
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
// проект ПЛК: System\PEData.plf проекта TIA или папка карты ПЛК с SIMATIC.S7S (подменяется FLEXRT_PLC)
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

// Весь запуск: пути и переменные окружения — в лог, затем ExtractionRun с выводом в консоль
// и лог (ConsoleReporter). Возвращает код возврата: 0 — без предупреждений и ошибок, 1 — иначе.
int RunAll()
{
    Log.Information("Запуск FlexRt: панель {Panel} (FLEXRT_INPUT: {InputOverride}), проект ПЛК {Plc} (FLEXRT_PLC: {PlcOverride}), каталог результата {Out} (FLEXRT_OUT: {OutOverride})",
        fwxPath, inputOverride ?? "не задана", plcPath, plcOverride ?? "не задана",
        Path.GetDirectoryName(stringsWorkbookPath), outOverride ?? "не задана");
    var paths = new ExtractionPaths(fwxPath, plcPath, stringsWorkbookPath, panelWorkbookPath, hexDir);
    return ExtractionRun.Execute(paths, new ConsoleReporter()).ExitCode;
}

// Путь выхода: без FLEXRT_OUT — путь по умолчанию (константа) как есть; с FLEXRT_OUT — фиксированное
// имя nameInOut внутри FLEXRT_OUT. Константы на результат с FLEXRT_OUT не влияют, поэтому ни один
// выход не уходит мимо FLEXRT_OUT, даже если константу перенести на другой диск.
string OutPath(string defaultPath, string nameInOut) => string.IsNullOrWhiteSpace(outOverride)
    ? defaultPath
    : Path.Combine(outOverride, nameInOut);
