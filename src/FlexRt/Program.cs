using System.Text;
using FlexRt.Binary;
using FlexRt.Export;
using FlexRt.Model;
using FlexRt.Parsing;

// ---- статичные пути: поменяй под себя ----
const string defaultFwxPath = @"C:\Users\Alexander\Desktop\extract_backup_data\Project1\IM\HMI\C\0\Generates\pdata.fwc";
const string xlsxPath = @"D:\projects\extract_backup\data\out\PDATA.xlsx";
const string explodeDir = @"D:\projects\extract_backup\data\out\pdata";
const string panelDataPath = @"D:\projects\extract_backup\data\out\panel_data.xlsx";
// ------------------------------------------

// входной файл: из переменной окружения FLEXRT_INPUT, если она задана и не пуста, иначе defaultFwxPath
var inputOverride = Environment.GetEnvironmentVariable("FLEXRT_INPUT");
var fwxPath = string.IsNullOrWhiteSpace(inputOverride) ? defaultFwxPath : inputOverride;

Console.OutputEncoding = Encoding.UTF8;

FwxDocument doc;
try
{
    doc = FwxReader.Read(fwxPath);
}
catch (Exception e) when (e is FwxFormatException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Не удалось прочитать {fwxPath}: {e.Message}");
    return 1;
}

Console.WriteLine($"Файл:    {fwxPath} ({doc.Binary.Length} байт)");
Console.WriteLine($"Таблиц:  {doc.Toc.Count}");
foreach (var t in doc.Toc)
{
    Console.WriteLine($"  0x{t.Id,-4:x} {t.Name,-24} элементов: {t.Entries,-6} offset: 0x{t.Offset:x} size: {t.Size}");
}
Console.WriteLine($"Строк языков: {doc.Strings.Count}");
Console.WriteLine($"Тегов: {doc.Tags.Count}");
foreach (var warning in doc.Warnings)
{
    Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: не разобрано - {warning}");
}

var failed = doc.Warnings.Count;

Run("Языки -> XLSX", () => WriteWorkbook(xlsxPath, doc.Strings.Count == 0, "в STRINGSTORE нет строк, пропуск",
    () => $"{XlsxExporter.Export(doc.Strings, xlsxPath)} языков, {doc.Strings.Count} строк -> {xlsxPath}"));

Run("Теги -> XLSX", () => WriteWorkbook(panelDataPath, doc.Tags.Count == 0, "в VAR нет тегов, пропуск",
    () => $"{PanelDataExporter.Export(doc.Tags, panelDataPath)} тегов -> {panelDataPath}"));

Run("Explode -> HEX", () =>
{
    var warnings = new List<string>();
    var files = HexExporter.Export(doc, explodeDir, warnings);
    foreach (var warning in warnings)
    {
        failed++;
        Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: таблица сохранена целиком (raw.hex.txt) - {warning}");
    }
    return $"{files} файлов -> {explodeDir}";
});

return failed == 0 ? 0 : 1;

// Выполнить экспорт и напечатать его итог. Экспорты независимы: сбой одного не отменяет остальные.
void Run(string title, Func<string> action)
{
    try
    {
        Console.WriteLine($"[{title}] {action()}");
    }
    catch (Exception e)
    {
        failed++;
        Console.Error.WriteLine($"[{title}] ОШИБКА: {e.Message}");
    }
}

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
