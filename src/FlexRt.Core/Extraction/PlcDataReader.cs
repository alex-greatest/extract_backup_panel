using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Matching;
using FlexRt.Parsing.Plc.Card;
using FlexRt.Parsing.Plc.Plf;

namespace FlexRt.Extraction;

/// <summary>
/// Данные ПЛК для прочитанной панели: проверка «не поддерживается», чтение проекта ПЛК, сопоставление тегов и
/// сообщения о проблемах проекта ПЛК. Шаг открытия панели в окне; сам ничего
/// не печатает — строки уходят в текст запуска <see cref="RunTranscript"/>.
/// </summary>
public sealed class PlcDataReader
{
    /// <summary>
    /// Конец сообщения «не поддерживается»: что стало с результатом — окно показывает панель без данных ПЛК (решение
    /// пользователя).
    /// </summary>
    public const string UnsupportedConsequence = "панель показана без данных ПЛК";

    /// <summary>Проект ПЛК: файл <c>PEData.plf</c> или папка карты ПЛК; может не существовать.</summary>
    private readonly string _plcPath;

    /// <summary>Текст запуска.</summary>
    private readonly RunTranscript _transcript;

    /// <summary>Сообщения о проблемах проекта ПЛК (вкладка «Ошибки» окна).</summary>
    private readonly List<string> _errors = [];

    /// <summary>Создаёт чтение; выполняет его <see cref="Read"/>.</summary>
    private PlcDataReader(string plcPath, RunTranscript transcript)
    {
        _plcPath = plcPath;
        _transcript = transcript;
    }

    /// <summary>
    /// Прочитать данные ПЛК для панели. Порядок событий: при нескольких соединениях панели и заданных данных ПЛК —
    /// <see cref="RunTranscript.Unsupported"/> (с концом <see cref="UnsupportedConsequence"/>) и конец; проект ПЛК
    /// (не найден, прочитан или не прочитан); по <see cref="RunTranscript.PlcError"/> на проблему проекта ПЛК.
    /// </summary>
    /// <param name="doc">Прочитанная панель.</param>
    /// <param name="plcPath">Файл <c>PEData.plf</c> или папка карты ПЛК; может не существовать.</param>
    /// <param name="transcript">Текст запуска: строки данных ПЛК.</param>
    /// <returns>Проект ПЛК, откуда данные ПЛК, сопоставление и ошибки ПЛК; при «не поддерживается» — только признак.</returns>
    public static PlcData Read(FwxDocument doc, string plcPath, RunTranscript transcript) =>
        new PlcDataReader(plcPath, transcript).Run(doc);

    /// <summary>Всё чтение; см. <see cref="Read"/>.</summary>
    /// <returns>Итог чтения.</returns>
    private PlcData Run(FwxDocument doc)
    {
        if (IsUnsupported(doc))
        {
            return new PlcData(true, null, null, [], []);
        }
        var (project, source) = ReadPlcProject();
        var matches = MatchTags(doc, project);
        foreach (var error in _errors)
        {
            _transcript.PlcError(error);
        }
        return new PlcData(false, project, source, matches, _errors);
    }

    /// <summary>
    /// Сознательно не поддерживается (решение пользователя): у панели больше одного соединения с
    /// ПЛК и данные ПЛК заданы — есть файл <c>PEData.plf</c> или папка карты ПЛК. ПЛК для
    /// соединения тогда нужно выбирать по IP, а это проверено не на всех источниках. Сообщает
    /// <see cref="RunTranscript.Unsupported"/>; без данных ПЛК такая панель читается.
    /// </summary>
    /// <returns><c>true</c>, если данные ПЛК не читаются.</returns>
    private bool IsUnsupported(FwxDocument doc)
    {
        if (doc.Connections.Count <= 1 || (!File.Exists(_plcPath) && !Directory.Exists(_plcPath)))
        {
            return false;
        }
        _transcript.Unsupported($"не поддерживается: соединений панели с ПЛК - {doc.Connections.Count}, а данные ПЛК заданы ({_plcPath}) - "
            + $"сопоставление сделано только для панели с одним соединением; {UnsupportedConsequence}");
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
        var isCard = Directory.Exists(_plcPath);
        if (!isCard && !File.Exists(_plcPath))
        {
            _transcript.PlcNotFound(_plcPath);
            return (null, $"не найден: {_plcPath}");
        }
        try
        {
            var project = isCard ? CardReader.Read(_plcPath) : PlfReader.Read(_plcPath);
            _transcript.PlcRead(_plcPath, isCard, project);
            _errors.AddRange(project.Problems);
            return (project, $"{(isCard ? "карта ПЛК" : "PEData.plf")}: {_plcPath}");
        }
        catch (Exception e)
        {
            _transcript.PlcReadFailed(_plcPath, e);
            _errors.Add($"{_plcPath}: {ReadFailure.Describe(e)}");
            return (null, $"не прочитан: {_plcPath}");
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
            _errors.Add($"сопоставление с ПЛК: {ReadFailure.Describe(e)}");
            matches = TagMatcher.Match(doc, null);
        }
        return matches;
    }
}
