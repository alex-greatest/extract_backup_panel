using System.IO;
using FlexRt.Export.PanelData;
using FlexRt.Export.Strings;
using FlexRt.Extraction;
using FlexRt.Model.Panel;
using FlexRt.Ui.Panel.Sections;
using Radzen;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>
/// Сохранение в Excel из окна: вся книга <c>panel_data.xlsx</c>, один её лист или книга текстов <c>PDATA.xlsx</c> —
/// диалогом «Сохранить как». Помнит папку последнего сохранения до закрытия окна; до первого сохранения диалог
/// открывается в папке <c>pdata.fwc</c> (решение пользователя). Методы вызывать из потока окна: запись идёт вне его.
/// </summary>
public sealed class ExcelSaver
{
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
            return new SaveOutcome(NotificationSeverity.Warning, $"{PanelDataExporter.FileName} не записан", "ни один раздел не построен");
        }
        var outcome = await SaveAsync(PanelDataExporter.FileName, panelPath, path => ReportSheetsExporter.Export(sheets, path));
        return outcome is { Severity: NotificationSeverity.Success } && skipped.Count > 0
            ? new SaveOutcome(NotificationSeverity.Warning, "Сохранено не всё",
                $"{outcome.Detail}; не записаны листы (раздел не построен): {string.Join(", ", skipped)}")
            : outcome;
    }

    /// <summary>Сохранить один лист раздела в свою книгу; имя файла по умолчанию — имя листа: <c>Теги.xlsx</c>.</summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    public Task<SaveOutcome?> SaveSheetAsync(PanelSection section, string panelPath)
    {
        var sheet = PanelDataSheets.Sheet(section);
        return SaveAsync($"{section.Sheet}.xlsx", panelPath, path => ReportSheetsExporter.Export([sheet], path));
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
            return new SaveOutcome(NotificationSeverity.Warning, $"{XlsxExporter.FileName} не записан", "в STRINGSTORE нет строк");
        }
        return await SaveAsync(XlsxExporter.FileName, panelPath, path => XlsxExporter.Export(strings, path));
    }

    /// <summary>
    /// Спросить файл диалогом и записать его вне потока окна. Папка выбранного файла запоминается и при ошибке записи;
    /// сбой самого диалога — ошибка «диалог сохранения: причина».
    /// </summary>
    /// <returns>Итог: «Сохранено» с путём или ошибка диалога или записи; отменено — <c>null</c>.</returns>
    private async Task<SaveOutcome?> SaveAsync(string fileName, string panelPath, Action<string> write)
    {
        string? path;
        try
        {
            path = ExcelSaveDialog.Ask(fileName, _folder ?? Path.GetDirectoryName(panelPath));
        }
        catch (Exception e)
        {
            return new SaveOutcome(NotificationSeverity.Error, "Не удалось сохранить", $"диалог сохранения: {e.Message}");
        }
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
            return new SaveOutcome(NotificationSeverity.Error, "Не удалось сохранить", $"{path}: {ReadFailure.Describe(e)}");
        }
        return new SaveOutcome(NotificationSeverity.Success, "Сохранено", path);
    }
}
