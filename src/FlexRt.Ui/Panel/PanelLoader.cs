using FlexRt.Export.PanelData.Screens;
using FlexRt.Extraction;
using FlexRt.Parsing.Panel;
using FlexRt.Ui.Panel.Screens;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Panel;

/// <summary>
/// Открытие панели для окна по шагам: чтение <c>pdata.fwc</c>, текст запуска (<see cref="RunTranscript"/>), экраны,
/// данные ПЛК, разделы. Файлов не пишет; состояние окна не трогает — итог в <see cref="LoadedPanel"/>.
/// </summary>
public static class PanelLoader
{
    /// <summary>
    /// Чем кончается сообщение «не поддерживается» в окне: окно файлов не пишет, а панель показывает без данных ПЛК
    /// (решение пользователя; в консоли — «файлы не созданы»).
    /// </summary>
    private const string UnsupportedConsequence = "панель показана без данных ПЛК";

    /// <summary>
    /// Прочитать панель <paramref name="path"/> с данными ПЛК <paramref name="plcPath"/>, если они выбраны. Чтение файла,
    /// данных ПЛК и построение разделов идут вне вызывающего потока, остальные шаги — в нём, по очереди: текст запуска
    /// одновременно из двух потоков не пишется. Не прочитан сам файл — «Файл не прочитан» и «Не удалось прочитать…» на
    /// Info; упал следующий шаг — панели нет, а ошибка называет шаг (решение пользователя): «ОШИБКА: данные ПЛК не
    /// обработаны: причина». Исключений не бросает.
    /// </summary>
    /// <param name="path">Файл <c>pdata.fwc</c>.</param>
    /// <param name="plcPath">Файл <c>PEData.plf</c> или папка карты ПЛК; <c>null</c> — без данных ПЛК.</param>
    /// <returns>Открытая панель или сбой с текстом ошибки.</returns>
    public static async Task<LoadedPanel> LoadAsync(string path, string? plcPath)
    {
        var transcript = new RunTranscript();
        string? failedStep = null;
        try
        {
            var doc = await Task.Run(() => FwxReader.Read(path));
            failedStep = "сводка панели не построена";
            transcript.PanelRead(path, doc);
            failedStep = "экраны не разобраны";
            var screens = ScreenItems.From(doc);
            var screenCount = ScreensTable.ScreenCount(doc);
            failedStep = "данные ПЛК не обработаны";
            var plc = plcPath is null ? null : await Task.Run(() => PlcDataReader.Read(doc, plcPath, transcript, UnsupportedConsequence));
            failedStep = "разделы не построены";
            var view = await Task.Run(() => PanelViews.From(doc, path, plc));
            IReadOnlyList<TranscriptLine> lines =
                [.. transcript.Lines, .. PanelViews.Errors(view).Select(e => new TranscriptLine(TranscriptLevel.Warning, e))];
            return new LoadedPanel(screens, view, doc.Strings, screenCount, lines, null);
        }
        catch (Exception e)
        {
            return Failure(path, transcript, failedStep, e);
        }
    }

    /// <summary>
    /// Сбой открытия: шаг не задан — не прочитан сам файл («Файл не прочитан» и строка
    /// <see cref="RunTranscript.PanelReadFailed"/>); иначе — «ОШИБКА: шаг: причина» для строки состояния и последней
    /// строкой Info, после уже собранного текста.
    /// </summary>
    /// <returns>Итог без панели: строки Info и текст ошибки.</returns>
    private static LoadedPanel Failure(string path, RunTranscript transcript, string? failedStep, Exception e)
    {
        if (failedStep is null)
        {
            transcript.PanelReadFailed(path, e);
            return new LoadedPanel([], null, [], 0, transcript.Lines, $"Файл не прочитан: {ReadFailure.Describe(e)}");
        }
        var error = $"ОШИБКА: {failedStep}: {ReadFailure.Describe(e)}";
        return new LoadedPanel([], null, [], 0, [.. transcript.Lines, new TranscriptLine(TranscriptLevel.Error, error)], error);
    }
}
