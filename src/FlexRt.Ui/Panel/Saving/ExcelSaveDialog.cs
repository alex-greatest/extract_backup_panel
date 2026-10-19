using Microsoft.Win32;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>Выбор файла книги Excel для сохранения стандартным диалогом Windows «Сохранить как».</summary>
public static class ExcelSaveDialog
{
    /// <summary>Фильтр диалога: книга Excel.</summary>
    private const string Filter = "Книга Excel (*.xlsx)|*.xlsx";

    /// <summary>
    /// Показать диалог сохранения с именем <paramref name="fileName"/> в папке <paramref name="folder"/>. Файл уже есть —
    /// Windows спрашивает о перезаписи. Вызывать из потока окна.
    /// </summary>
    /// <returns>Полный путь выбранного файла или <c>null</c>, если сохранение отменено.</returns>
    public static string? Ask(string fileName, string? folder)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить в Excel",
            Filter = Filter,
            FileName = fileName,
            DefaultExt = ".xlsx",
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = folder ?? ""
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
