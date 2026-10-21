using FlexRt.Export.PanelData;
using FlexRt.Export.Pictures;
using FlexRt.Export.Strings;
using FlexRt.Extraction;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Открытие панели и сохранение её файлов внутри процесса теста — шагами ядра, как это делает окно: чтение
/// <c>pdata.fwc</c> с текстом запуска (<see cref="RunTranscript"/>), данные ПЛК (<see cref="PlcDataReader"/>, если они
/// выбраны), данные листов (<see cref="PanelSources.From"/>), книга <c>panel_data.xlsx</c>
/// (<see cref="PanelDataBook.TryBuild"/>, <see cref="ReportSheetsExporter"/>); по запросу теста — <c>PDATA.xlsx</c>
/// (<see cref="SaveStrings"/>) и архив картинок <c>images.zip</c> (<see cref="SavePictures"/>).
/// </summary>
public static class PanelRunner
{
    /// <summary>
    /// Прочитать панель с данными ПЛК и записать <c>panel_data.xlsx</c> в <paramref name="outDir"/>. Как в окне: любая
    /// ошибка чтения файла — строка «Не удалось прочитать…», файлы не пишутся; без выбранных данных ПЛК шаг ПЛК
    /// пропускается. В отличие от окна лист, который не
    /// построен, — провал теста, а не пропуск листа. Каталога результата до запуска быть не должно.
    /// </summary>
    /// <param name="input">Путь к <c>pdata.fwc</c>.</param>
    /// <param name="plc">Путь данных ПЛК (файл или папка карты; может не существовать); <c>null</c> — без данных ПЛК.</param>
    /// <param name="outDir">Каталог результата.</param>
    /// <returns>Текст запуска и прочитанная панель.</returns>
    public static PanelRun Run(string input, string? plc, string outDir)
    {
        Assert.False(Directory.Exists(outDir), $"каталог результата уже есть до запуска: {outDir}");
        var transcript = new RunTranscript();
        FwxDocument doc;
        try
        {
            doc = FwxReader.Read(input);
        }
        catch (Exception e)
        {
            transcript.PanelReadFailed(input, e);
            return Result(transcript, null);
        }
        transcript.PanelRead(input, doc);
        var data = plc is null ? null : PlcDataReader.Read(doc, plc, transcript);
        Directory.CreateDirectory(outDir);
        WritePanelData(PanelSources.From(doc, input, data), Path.Combine(outDir, PanelDataBook.FileName));
        return Result(transcript, doc);
    }

    /// <summary>Записать книгу строк языков прочитанной панели, как кнопка «PDATA.xlsx…» окна.</summary>
    /// <returns>Путь книги (<see cref="TestDirectory.StringsWorkbook"/>).</returns>
    public static string SaveStrings(PanelRun run, TestDirectory dir)
    {
        XlsxExporter.Export(Document(run).Strings, dir.StringsWorkbook);
        return dir.StringsWorkbook;
    }

    /// <summary>Записать все картинки прочитанной панели архивом, как пункт «Все в zip…» окна.</summary>
    /// <returns>Путь архива (<see cref="TestDirectory.PicturesArchive"/>).</returns>
    public static string SavePictures(PanelRun run, TestDirectory dir)
    {
        PictureArchiveExporter.Export(Document(run).Pictures, dir.PicturesArchive);
        return dir.PicturesArchive;
    }

    /// <summary>Записать книгу данных панели: все листы описания книги построены, иначе — провал теста.</summary>
    private static void WritePanelData(PanelDataSource source, string path)
    {
        var built = PanelDataBook.TryBuild(source);
        Assert.All(built, b => Assert.True(b.Error is null, $"лист «{b.Sheet.Name}» не построен: {b.Error}"));
        ReportSheetsExporter.Export([.. built.Select(b => b.Sheet)], path);
    }

    /// <summary>Прочитанная панель запуска; не прочитана — провал теста.</summary>
    /// <returns>Панель.</returns>
    private static FwxDocument Document(PanelRun run) => run.Document ?? throw new InvalidOperationException("панель не прочитана");

    /// <summary>Итог запуска: строки Info, предупреждения и ошибки текста запуска через перевод строки.</summary>
    /// <returns>Итог запуска.</returns>
    private static PanelRun Result(RunTranscript transcript, FwxDocument? doc) => new(
        string.Join("\n", transcript.Lines.Where(l => l.Level == TranscriptLevel.Info).Select(l => l.Text)),
        string.Join("\n", transcript.Lines.Where(l => l.Level != TranscriptLevel.Info).Select(l => l.Text)),
        doc);
}
