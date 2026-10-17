using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Extraction;

/// <summary>
/// Получатель событий запуска <see cref="ExtractionRun"/> и шага данных ПЛК <see cref="PlcDataReader"/>: лог консоли
/// или текст запуска (<see cref="RunTranscript"/>). События приходят в порядке, описанном у
/// <see cref="ExtractionRun.Execute"/> и <see cref="PlcDataReader.Read"/>.
/// </summary>
public interface IExtractionReporter
{
    /// <summary><c>pdata.fwc</c> не прочитан (ошибка формата или чтения файла); запуск прекращён.</summary>
    void PanelReadFailed(string path, Exception error);

    /// <summary>Панель прочитана; предупреждения разбора — в <see cref="FwxDocument.Warnings"/>.</summary>
    void PanelRead(string path, FwxDocument doc);

    /// <summary>Сочетание входов не поддерживается: данные ПЛК не читаются; конец сообщения говорит, что стало с результатом.</summary>
    void Unsupported(string message);

    /// <summary>Проекта ПЛК по пути нет: данные ПЛК неизвестны, ошибкой не считается.</summary>
    void PlcNotFound(string path);

    /// <summary>Проект ПЛК прочитан: из карты ПЛК (<paramref name="isCard"/>) или из <c>PEData.plf</c>.</summary>
    void PlcRead(string path, bool isCard, PlcProject project, long elapsedMs);

    /// <summary>Проект ПЛК не прочитан: данные ПЛК неизвестны; сообщение придёт ещё и в <see cref="PlcError"/>.</summary>
    void PlcReadFailed(string path, Exception error);

    /// <summary>Непредвиденная ошибка сопоставления; сопоставление повторено без проекта ПЛК.</summary>
    void MatchFailed(Exception error);

    /// <summary>Теги сопоставлены с ПЛК; <c>null</c> — внутренний тег.</summary>
    void Matched(IReadOnlyList<PlcMatch?> matches);

    /// <summary>Проблема проекта ПЛК — по событию на сообщение листа «Ошибки».</summary>
    void PlcError(string message);

    /// <summary>Таблица не подошла под «каталог смещений» и сохранена целиком в <c>raw.hex.txt</c>.</summary>
    void RawTableSaved(string warning);

    /// <summary>Экспорт закончен успешно или с ошибкой.</summary>
    void ExportFinished(ExportOutcome outcome);
}
