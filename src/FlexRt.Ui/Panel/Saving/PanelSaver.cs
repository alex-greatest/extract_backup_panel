using System.IO;
using FlexRt.Export.PanelData;
using FlexRt.Export.Pictures;
using FlexRt.Export.Strings;
using FlexRt.Extraction;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Pictures;
using FlexRt.Ui.Panel.Pictures;
using FlexRt.Ui.Panel.Sections;
using Radzen;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>
/// Сохранение из окна диалогом «Сохранить как»: вся книга <c>panel_data.xlsx</c>, один её лист, книга текстов
/// <c>PDATA.xlsx</c>, одна картинка или все картинки архивом <c>images.zip</c>. Помнит папку последнего сохранения до
/// закрытия окна; до первого сохранения диалог открывается в папке <c>pdata.fwc</c> (решение пользователя). Файл пишется
/// рядом во временный и заменяет выбранный только после удачной записи. Вызывать из потока окна: запись идёт вне его.
/// </summary>
public sealed class PanelSaver
{
    /// <summary>Заголовок диалога сохранения книг Excel.</summary>
    private const string ExcelTitle = "Сохранить в Excel";

    /// <summary>Фильтр диалога: книга Excel.</summary>
    private const string ExcelFilter = "Книга Excel (*.xlsx)|*.xlsx";

    /// <summary>Папка последнего сохранения; <c>null</c> — ещё не сохраняли.</summary>
    private string? _folder;

    /// <summary>
    /// Сохранить пункт панели инструментов (<see cref="SaveItem"/>): лист — только у построенного раздела книги,
    /// картинку — только выбранную в галерее, иначе ничего не делает; неизвестный пункт — вся книга.
    /// </summary>
    /// <returns>Итог; сохранять нечего или сохранение отменено — <c>null</c>.</returns>
    public async Task<SaveOutcome?> SaveAsync(string? item, SaveSources sources, string panelPath) => item switch
    {
        SaveItem.Sheet => sources.Selected is PanelSection section && PanelDataSheets.IsSheet(sources.View, section)
            ? await SaveSheetAsync(section, panelPath)
            : null,
        SaveItem.Strings => await SaveStringsAsync(sources.Strings, panelPath),
        SaveItem.Picture => sources.Pictures.Selected is { } picture ? await SavePictureAsync(picture, panelPath) : null,
        SaveItem.Pictures => await SavePicturesAsync(sources.Pictures.Pictures, panelPath),
        _ => await SavePanelDataAsync(sources.View, panelPath)
    };

    /// <summary>
    /// Сохранить всю книгу данных панели (<see cref="PanelDataSheets.Book"/>): разделы, которые не построены, не пишутся
    /// и называются в итоге. Не построено ни одного — диалог не показывается.
    /// </summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    private async Task<SaveOutcome?> SavePanelDataAsync(PanelView view, string panelPath)
    {
        var (sheets, skipped) = PanelDataSheets.Book(view);
        if (sheets.Count == 0)
        {
            return new SaveOutcome(NotificationSeverity.Warning, $"{PanelDataExporter.FileName} не записан", "ни один раздел не построен");
        }
        var outcome = await SaveExcelAsync(PanelDataExporter.FileName, panelPath, path => ReportSheetsExporter.Export(sheets, path));
        return outcome is { Severity: NotificationSeverity.Success } && skipped.Count > 0
            ? new SaveOutcome(NotificationSeverity.Warning, "Сохранено не всё",
                $"{outcome.Detail}; не записаны листы (раздел не построен): {string.Join(", ", skipped)}")
            : outcome;
    }

    /// <summary>Сохранить один лист раздела в свою книгу; имя файла по умолчанию — имя листа: <c>Теги.xlsx</c>.</summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    private Task<SaveOutcome?> SaveSheetAsync(PanelSection section, string panelPath)
    {
        var sheet = PanelDataSheets.Sheet(section);
        return SaveExcelAsync($"{section.Sheet}.xlsx", panelPath, path => ReportSheetsExporter.Export([sheet], path));
    }

    /// <summary>
    /// Сохранить книгу текстов тем же экспортёром, что у консоли (<see cref="XlsxExporter.Export"/>). Строк нет — диалог не
    /// показывается: книгу без листов Excel не сохраняет.
    /// </summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    private async Task<SaveOutcome?> SaveStringsAsync(IReadOnlyList<LangString> strings, string panelPath)
    {
        if (strings.Count == 0)
        {
            return new SaveOutcome(NotificationSeverity.Warning, $"{XlsxExporter.FileName} не записан", "в STRINGSTORE нет строк");
        }
        return await SaveExcelAsync(XlsxExporter.FileName, panelPath, path => XlsxExporter.Export(strings, path));
    }

    /// <summary>
    /// Сохранить одну картинку: байты файла как есть, имя по умолчанию — как у файла консоли (<c>PICTURE_5.png</c>), фильтр
    /// диалога — её формат.
    /// </summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    private Task<SaveOutcome?> SavePictureAsync(PictureItem picture, string panelPath)
    {
        var extension = picture.Picture.Extension;
        var filter = extension switch
        {
            PictureFormat.Png => "Картинка PNG (*.png)|*.png",
            PictureFormat.Jpg => "Картинка JPEG (*.jpg)|*.jpg",
            _ => $"Файл (*.{extension})|*.{extension}"
        };
        return SaveAsync("Сохранить картинку", filter, picture.Name, panelPath, path => File.WriteAllBytes(path, picture.Picture.Data));
    }

    /// <summary>
    /// Сохранить все картинки панели архивом (<see cref="PictureArchiveExporter.Export"/>): файлы в корне архива под
    /// именами консоли. Вызывать, только если картинки есть: меню «Картинки» без них недоступно.
    /// </summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    private Task<SaveOutcome?> SavePicturesAsync(PanelPictures pictures, string panelPath)
    {
        var data = pictures.Items.Select(p => p.Picture).ToList();
        return SaveAsync("Сохранить картинки в архив", "Архив zip (*.zip)|*.zip", PictureArchiveExporter.FileName, panelPath,
            path => PictureArchiveExporter.Export(data, path));
    }

    /// <summary>Сохранить книгу Excel: диалог «Сохранить в Excel» с фильтром <c>*.xlsx</c>.</summary>
    /// <returns>Итог; сохранение отменено — <c>null</c>.</returns>
    private Task<SaveOutcome?> SaveExcelAsync(string fileName, string panelPath, Action<string> write) =>
        SaveAsync(ExcelTitle, ExcelFilter, fileName, panelPath, write);

    /// <summary>
    /// Спросить файл диалогом и записать его вне потока окна (<see cref="WriteReplacing"/>). Папка выбранного файла
    /// запоминается и при ошибке записи; сбой самого диалога — ошибка «диалог сохранения: причина».
    /// </summary>
    /// <returns>Итог: «Сохранено» с путём или ошибка диалога или записи; отменено — <c>null</c>.</returns>
    private async Task<SaveOutcome?> SaveAsync(string title, string filter, string fileName, string panelPath, Action<string> write)
    {
        string? path;
        try
        {
            path = SaveDialog.Ask(title, filter, fileName, _folder ?? Path.GetDirectoryName(panelPath));
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
            await Task.Run(() => WriteReplacing(path, write));
        }
        catch (Exception e)
        {
            return new SaveOutcome(NotificationSeverity.Error, "Не удалось сохранить", $"{path}: {ReadFailure.Describe(e)}");
        }
        return new SaveOutcome(NotificationSeverity.Success, "Сохранено", path);
    }

    /// <summary>
    /// Записать файл во временный рядом с <paramref name="path"/> (<c>имя.&lt;guid&gt;.расширение</c>: расширение остаётся
    /// последним — по нему ClosedXML выбирает формат) и только после удачной записи заменить им выбранный. Сбой записи
    /// не трогает прежний файл; временный удаляется в любом случае.
    /// </summary>
    /// <exception cref="IOException">Временный файл не записан или выбранный не заменён (например, открыт в Excel).</exception>
    private static void WriteReplacing(string path, Action<string> write)
    {
        var temp = Path.Combine(Path.GetDirectoryName(path) ?? "",
            $"{Path.GetFileNameWithoutExtension(path)}.{Guid.NewGuid():N}{Path.GetExtension(path)}");
        try
        {
            write(temp);
            File.Move(temp, path, true);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
