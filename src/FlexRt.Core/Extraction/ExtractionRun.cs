using System.Diagnostics;
using FlexRt.Binary;
using FlexRt.Export.Hex;
using FlexRt.Export.PanelData;
using FlexRt.Export.Pictures;
using FlexRt.Export.Strings;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;
using FlexRt.Parsing.Matching;
using FlexRt.Parsing.Panel;
using FlexRt.Parsing.Plc.Card;
using FlexRt.Parsing.Plc.Plf;

namespace FlexRt.Extraction;

/// <summary>
/// Один запуск извлечения: чтение панели, проекта ПЛК, сопоставление тегов и четыре независимых
/// экспорта. Сам ничего не печатает: всё, что видит пользователь, уходит в
/// <see cref="IExtractionReporter"/>.
/// </summary>
public sealed class ExtractionRun
{
    /// <summary>Входы и выходы запуска.</summary>
    private readonly ExtractionPaths _paths;

    /// <summary>Получатель событий запуска.</summary>
    private readonly IExtractionReporter _reporter;

    /// <summary>Сообщения о проблемах проекта ПЛК (лист «Ошибки»).</summary>
    private readonly List<string> _plcErrors = [];

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
        catch (Exception e) when (IsReadError(e))
        {
            _reporter.PanelReadFailed(_paths.FwxPath, e);
            return Result(1, null, null, [], null);
        }

        _reporter.PanelRead(_paths.FwxPath, doc);
        if (IsUnsupported(doc))
        {
            return Result(1, doc, null, [], null);
        }
        var failed = doc.Warnings.Count;

        var (plcProject, plcSource) = ReadPlcProject();
        var matches = MatchTags(doc, plcProject);
        var summary = new RunSummary(_paths.FwxPath, PanelFilesReader.Read(_paths.FwxPath), plcSource);
        foreach (var error in _plcErrors)
        {
            failed++;
            _reporter.PlcError(error);
        }

        failed += Export(doc, plcProject, summary, matches);
        return Result(failed == 0 ? 0 : 1, doc, plcProject, matches, summary);
    }

    /// <summary>Итог запуска с накопленными ошибками ПЛК и итогами экспортов.</summary>
    /// <returns>Итог запуска.</returns>
    private ExtractionResult Result(int code, FwxDocument? doc, PlcProject? project, IReadOnlyList<PlcMatch?> matches, RunSummary? summary) =>
        new(code, doc, project, matches, _plcErrors, summary, _exports);

    /// <summary>
    /// Сознательно не поддерживается (решение пользователя): у панели больше одного соединения с
    /// ПЛК и данные ПЛК заданы — есть файл <c>PEData.plf</c> или папка карты ПЛК. ПЛК для
    /// соединения тогда нужно выбирать по IP, а это проверено не на всех источниках. Сообщает
    /// <see cref="IExtractionReporter.Unsupported"/>; без данных ПЛК такая панель читается.
    /// </summary>
    /// <returns><c>true</c>, если запуск нужно прекратить.</returns>
    private bool IsUnsupported(FwxDocument doc)
    {
        if (doc.Connections.Count <= 1 || (!File.Exists(_paths.PlcPath) && !Directory.Exists(_paths.PlcPath)))
        {
            return false;
        }
        _reporter.Unsupported($"не поддерживается: соединений панели с ПЛК - {doc.Connections.Count}, а данные ПЛК заданы ({_paths.PlcPath}) - "
            + "сопоставление сделано только для панели с одним соединением; файлы не созданы");
        return true;
    }

    /// <summary>
    /// Прочитать проект ПЛК. Путь — папка: это карта ПЛК (<c>SIMATIC.S7S</c>), иначе файл
    /// <c>PEData.plf</c>. Нет ни папки, ни файла — <c>null</c>, ошибкой не считается. Файл или
    /// карта сломаны, не открываются или разбор упал непредвиденно — сообщение в ошибки ПЛК
    /// (непредвиденная — с типом ошибки), <c>null</c>: данные панели всё равно выгружаются. Теги
    /// ПЛК, которые не удалось разобрать, — сообщения в ошибки ПЛК, проект при этом возвращается.
    /// </summary>
    /// <returns>Проект ПЛК и откуда данные ПЛК для листа «Сводка»: «карта ПЛК: путь», «PEData.plf: путь», «не найден: путь», «не прочитан: путь».</returns>
    private (PlcProject? Project, string Source) ReadPlcProject()
    {
        var path = _paths.PlcPath;
        var isCard = Directory.Exists(path);
        if (!isCard && !File.Exists(path))
        {
            _reporter.PlcNotFound(path);
            return (null, $"не найден: {path}");
        }
        var timer = Stopwatch.StartNew();
        try
        {
            var project = isCard ? CardReader.Read(path) : PlfReader.Read(path);
            _reporter.PlcRead(path, isCard, project, timer.ElapsedMilliseconds);
            _plcErrors.AddRange(project.Problems);
            return (project, $"{(isCard ? "карта ПЛК" : "PEData.plf")}: {path}");
        }
        catch (Exception e)
        {
            _reporter.PlcReadFailed(path, e);
            _plcErrors.Add($"{path}: {Describe(e)}");
            return (null, $"не прочитан: {path}");
        }
    }

    /// <summary>
    /// Сопоставить теги панели с проектом ПЛК. Непредвиденная ошибка сопоставления — сообщение
    /// в ошибки ПЛК и сопоставление без проекта ПЛК (колонки ПЛК «неизвестно»).
    /// </summary>
    /// <returns>Результат по порядку тегов; <c>null</c> — внутренний тег.</returns>
    private List<PlcMatch?> MatchTags(FwxDocument doc, PlcProject? project)
    {
        List<PlcMatch?> matches;
        try
        {
            matches = TagMatcher.Match(doc, project);
        }
        catch (Exception e)
        {
            _reporter.MatchFailed(e);
            _plcErrors.Add($"сопоставление с ПЛК: {Describe(e)}");
            matches = TagMatcher.Match(doc, null);
        }
        _reporter.Matched(matches);
        return matches;
    }

    /// <summary>
    /// Четыре независимых экспорта: строки языков, данные панели, hex всех таблиц, картинки. Нет строк или
    /// тегов — соответствующая книга не пишется, файл прошлого запуска удаляется.
    /// </summary>
    /// <returns>Число сбоев: упавшие экспорты плюс таблицы, сохранённые целиком в <c>raw.hex.txt</c>.</returns>
    private int Export(FwxDocument doc, PlcProject? project, RunSummary summary, IReadOnlyList<PlcMatch?> matches)
    {
        var failed = RunExport("Языки -> XLSX", () => WriteWorkbook(_paths.StringsWorkbookPath, doc.Strings.Count == 0, "в STRINGSTORE нет строк, пропуск",
            () => $"{XlsxExporter.Export(doc.Strings, _paths.StringsWorkbookPath)} языков, {doc.Strings.Count} строк -> {_paths.StringsWorkbookPath}"));

        failed += RunExport("Теги -> XLSX", () => WriteWorkbook(_paths.PanelWorkbookPath, doc.Tags.Count == 0, "в VAR нет тегов, пропуск",
            () => $"{PanelDataExporter.Export(doc, project, summary, matches, _plcErrors, _paths.PanelWorkbookPath)} тегов -> {_paths.PanelWorkbookPath}"));

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
    /// при сообщении об успехе (например, stdout не пишется) тоже считается ошибкой экспорта.
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

    /// <summary>
    /// Текст ошибки для листа «Ошибки»: ошибки формата и чтения — их сообщение,
    /// непредвиденная — «непредвиденная ошибка &lt;тип&gt;: &lt;сообщение&gt;».
    /// </summary>
    /// <returns>Текст сообщения.</returns>
    private static string Describe(Exception e) => IsReadError(e)
        ? e.Message
        : $"непредвиденная ошибка {e.GetType().Name}: {e.Message}";

    /// <summary>Ожидаемая ошибка чтения входа: ошибка формата или ввода-вывода, нет доступа к файлу.</summary>
    /// <returns><c>true</c> для <see cref="FwxFormatException"/>, <see cref="IOException"/> и <see cref="UnauthorizedAccessException"/>.</returns>
    private static bool IsReadError(Exception e) => e is FwxFormatException or IOException or UnauthorizedAccessException;
}
