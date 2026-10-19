using System.IO;
using FlexRt.Export.PanelData;
using FlexRt.Export.Strings;
using FlexRt.Extraction;
using FlexRt.Model.Panel;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>
/// Сохранение в Excel из окна: вся книга <c>panel_data.xlsx</c>, один её лист или книга текстов <c>PDATA.xlsx</c> —
/// диалогом «Сохранить как». Помнит папку последнего сохранения до закрытия окна; до первого сохранения диалог
/// открывается в папке <c>pdata.fwc</c> (решение пользователя). Методы вызывать из потока окна: запись идёт вне его.
/// </summary>
public sealed class ExcelSaver
{
    /// <summary>Имя книги данных панели по умолчанию — как у консоли.</summary>
    private const string PanelDataName = "panel_data.xlsx";

    /// <summary>Имя книги текстов по умолчанию — как у консоли.</summary>
    private const string StringsName = "PDATA.xlsx";

    /// <summary>Папка последнего сохранения; <c>null</c> — ещё не сохраняли.</summary>
    private string? _folder;

    /// <summary>
    /// Сохранить всю книгу данных панели (<see cref="PanelDataSheets.Book"/>): разделы, которые не построены, не пишутся
    /// и называются в итоге. Не построено ни одного — диалог не показывается.
    /// </summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    public async Task<SaveOutcome?> SavePanelDataAsync(PanelView view, string panelPath)
    {
        var (sheets, skipped) = PanelDataSheets.Book(view);
        if (sheets.Count == 0)
        {
            return new SaveOutcome(TranscriptLevel.Warning, $"{PanelDataName} не записан", "ни один раздел не построен");
        }
        return await SaveAsync(PanelDataName, panelPath, path => ReportSheetsExporter.Export(sheets, path), skipped);
    }

    /// <summary>Сохранить один лист раздела в свою книгу; имя файла по умолчанию — имя листа: <c>Теги.xlsx</c>.</summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    public Task<SaveOutcome?> SaveSheetAsync(PanelView view, PanelSection section, string panelPath)
    {
        var sheet = PanelDataSheets.Sheet(view, section);
        return SaveAsync($"{section.Sheet}.xlsx", panelPath, path => ReportSheetsExporter.Export([sheet], path), []);
    }

    /// <summary>
    /// Сохранить книгу текстов тем же экспортёром, что у консоли (<see cref="XlsxExporter.Export"/>). Строк нет — диалог не
    /// показывается: книгу без листов Excel не сохраняет.
    /// </summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    public async Task<SaveOutcome?> SaveStringsAsync(IReadOnlyList<LangString> strings, string panelPath)
    {
        if (strings.Count == 0)
        {
            return new SaveOutcome(TranscriptLevel.Warning, $"{StringsName} не записан", "в STRINGSTORE нет строк");
        }
        return await SaveAsync(StringsName, panelPath, path => XlsxExporter.Export(strings, path), []);
    }

    /// <summary>
    /// Спросить файл диалогом и записать его вне потока окна. Папка выбранного файла запоминается и при ошибке записи.
    /// </summary>
    /// <returns>Итог: записано, записано без пропущенных листов или ошибка записи; отменено — <c>null</c>.</returns>
    private async Task<SaveOutcome?> SaveAsync(string fileName, string panelPath, Func<string, int> write, IReadOnlyList<string> skipped)
    {
        var path = ExcelSaveDialog.Ask(fileName, _folder ?? Path.GetDirectoryName(panelPath));
        if (path is null)
        {
            return null;
        }
        _folder = Path.GetDirectoryName(path);
        try
        {
            await Task.Run(() => write(path));
        }
        catch (Exception e)
        {
            return new SaveOutcome(TranscriptLevel.Error, "Не удалось сохранить", $"{path}: {ReadFailure.Describe(e)}");
        }
        return skipped.Count == 0
            ? new SaveOutcome(TranscriptLevel.Info, "Сохранено", path)
            : new SaveOutcome(TranscriptLevel.Warning, "Сохранено не всё", $"{path}; не записаны листы (раздел не построен): {string.Join(", ", skipped)}");
    }
}
