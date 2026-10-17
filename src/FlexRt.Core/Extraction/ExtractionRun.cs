using System.Diagnostics;
using FlexRt.Export.Hex;
using FlexRt.Export.PanelData;
using FlexRt.Export.Pictures;
using FlexRt.Export.Strings;
using FlexRt.Model.Panel;
using FlexRt.Model.Summary;
using FlexRt.Parsing.Panel;

namespace FlexRt.Extraction;

/// <summary>
/// Один запуск извлечения: чтение панели, проекта ПЛК, сопоставление тегов и четыре независимых
/// экспорта. Сам ничего не печатает: всё, что видит пользователь, уходит в
/// <see cref="IExtractionReporter"/>.
/// </summary>
public sealed class ExtractionRun
{
    /// <summary>Конец сообщения «не поддерживается» у запуска: файлы при этом не пишутся.</summary>
    private const string UnsupportedConsequence = "файлы не созданы";

    /// <summary>Входы и выходы запуска.</summary>
    private readonly ExtractionPaths _paths;

    /// <summary>Получатель событий запуска.</summary>
    private readonly IExtractionReporter _reporter;

    /// <summary>Итоги экспортов в порядке выполнения.</summary>
    private readonly List<ExportOutcome> _exports = [];

    /// <summary>Создаёт запуск; выполняет его <see cref="Execute"/>.</summary>
    private ExtractionRun(ExtractionPaths paths, IExtractionReporter reporter)
    {
        _paths = paths;
        _reporter = reporter;
    }

    /// <summary>
    /// Выполнить запуск. Порядок событий: панель прочитана (или не прочитана — тогда код 1 и
    /// конец); при нескольких соединениях панели и заданных данных ПЛК — «не поддерживается»,
    /// код 1, файлы не создаются; проект ПЛК (не найден, прочитан или не прочитан);
    /// сопоставление; по событию на проблему проекта ПЛК; четыре экспорта — строки языков, данные
    /// панели, hex, картинки. Экспорты независимы: сбой одного не отменяет остальные.
    /// </summary>
    /// <returns>Код возврата (0 — без предупреждений и ошибок, 1 — иначе) и прочитанные данные.</returns>
    /// <exception cref="Exception">Непредвиденная ошибка чтения панели (не формата и не чтения файла) не перехватывается.</exception>
    public static ExtractionResult Execute(ExtractionPaths paths, IExtractionReporter reporter) =>
        new ExtractionRun(paths, reporter).Run();

    /// <summary>Весь запуск; см. <see cref="Execute"/>.</summary>
    /// <returns>Итог запуска.</returns>
    private ExtractionResult Run()
    {
        FwxDocument doc;
        try
        {
            doc = FwxReader.Read(_paths.FwxPath);
        }
        catch (Exception e) when (ReadFailure.IsReadError(e))
        {
            _reporter.PanelReadFailed(_paths.FwxPath, e);
            return Result(1, null, null, null);
        }

        _reporter.PanelRead(_paths.FwxPath, doc);
        var plc = PlcDataReader.Read(doc, _paths.PlcPath, _reporter, UnsupportedConsequence);
        if (plc.IsUnsupported)
        {
            return Result(1, doc, plc, null);
        }
        var summary = new RunSummary(_paths.FwxPath, PanelFilesReader.Read(_paths.FwxPath), plc.Source!);
        var failed = doc.Warnings.Count + plc.Errors.Count + Export(doc, plc, summary);
        return Result(failed == 0 ? 0 : 1, doc, plc, summary);
    }

    /// <summary>Итог запуска с данными ПЛК (<c>null</c> — до них не дошло) и итогами экспортов.</summary>
    /// <returns>Итог запуска.</returns>
    private ExtractionResult Result(int code, FwxDocument? doc, PlcData? plc, RunSummary? summary) =>
        new(code, doc, plc?.Project, plc?.Matches ?? [], plc?.Errors ?? [], summary, _exports);

    /// <summary>
    /// Четыре независимых экспорта: строки языков, данные панели, hex всех таблиц, картинки. Нет строк или
    /// тегов — соответствующая книга не пишется, файл прошлого запуска удаляется.
    /// </summary>
    /// <returns>Число сбоев: упавшие экспорты плюс таблицы, сохранённые целиком в <c>raw.hex.txt</c>.</returns>
    private int Export(FwxDocument doc, PlcData plc, RunSummary summary)
    {
        var failed = RunExport("Языки -> XLSX", () => WriteWorkbook(_paths.StringsWorkbookPath, doc.Strings.Count == 0, "в STRINGSTORE нет строк, пропуск",
            () => $"{XlsxExporter.Export(doc.Strings, _paths.StringsWorkbookPath)} языков, {doc.Strings.Count} строк -> {_paths.StringsWorkbookPath}"));

        failed += RunExport("Теги -> XLSX", () => WriteWorkbook(_paths.PanelWorkbookPath, doc.Tags.Count == 0, "в VAR нет тегов, пропуск",
            () => $"{PanelDataExporter.Export(doc, plc.Project, summary, plc.Matches, plc.Errors, _paths.PanelWorkbookPath)} тегов -> {_paths.PanelWorkbookPath}"));

        var hexWarnings = new List<string>();
        failed += RunExport("Explode -> HEX", () =>
        {
            var files = HexExporter.Export(doc, _paths.HexDir, hexWarnings);
            foreach (var warning in hexWarnings)
            {
                _reporter.RawTableSaved(warning);
            }
            return $"{files} файлов -> {_paths.HexDir}";
        });

        failed += RunExport("Картинки -> файлы", () => $"{PictureExporter.Export(doc, _paths.PicturesDir)} картинок -> {_paths.PicturesDir}");
        return failed + hexWarnings.Count;
    }

    /// <summary>
    /// Выполнить экспорт, замерить время и сообщить итог <see cref="IExtractionReporter.ExportFinished"/>.
    /// Любая ошибка экспорта перехватывается: остальные экспорты всё равно выполняются. Ошибка
    /// при сообщении об успехе (например, лог не пишется) тоже считается ошибкой экспорта.
    /// </summary>
    /// <returns>1 при ошибке, иначе 0.</returns>
    private int RunExport(string title, Func<string> action)
    {
        var timer = Stopwatch.StartNew();
        ExportOutcome outcome;
        try
        {
            var result = action();
            var success = new ExportOutcome(title, result, null, timer.ElapsedMilliseconds);
            _reporter.ExportFinished(success);
            _exports.Add(success);
            return 0;
        }
        catch (Exception e)
        {
            outcome = new ExportOutcome(title, null, e, timer.ElapsedMilliseconds);
        }
        _exports.Add(outcome);
        _reporter.ExportFinished(outcome);
        return outcome.Error is null ? 0 : 1;
    }

    /// <summary>
    /// Записать книгу XLSX. Нет данных — удалить файл прошлого запуска и вернуть сообщение о
    /// пропуске; иначе создать каталог файла и выполнить экспорт.
    /// </summary>
    /// <returns>Итог экспорта или <paramref name="skipMessage"/>.</returns>
    private static string WriteWorkbook(string path, bool isEmpty, string skipMessage, Func<string> export)
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
}
