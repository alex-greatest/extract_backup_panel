using System.Text;
using FlexRt.Binary;
using FlexRt.Export;
using FlexRt.Model;
using FlexRt.Parsing;

// ---- статичные пути: поменяй под себя ----
const string fwxPath = @"D:\projects\extract_backup\data\PDATA.fwx";
const string xlsxPath = @"D:\projects\extract_backup\data\out\PDATA.xlsx";
const string explodeDir = @"D:\projects\extract_backup\data\out\pdata";
// ------------------------------------------

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
foreach (var warning in doc.Warnings)
{
    Console.Error.WriteLine($"ПРЕДУПРЕЖДЕНИЕ: не разобрано - {warning}");
}

var failed = doc.Warnings.Count;

Run("Языки -> XLSX", () =>
{
    if (doc.Strings.Count == 0)
    {
        return "в STRINGSTORE нет строк, пропуск";
    }
    Directory.CreateDirectory(Path.GetDirectoryName(xlsxPath)!);
    var langs = XlsxExporter.Export(doc.Strings, xlsxPath);
    return $"{langs} языков, {doc.Strings.Count} строк -> {xlsxPath}";
});

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
