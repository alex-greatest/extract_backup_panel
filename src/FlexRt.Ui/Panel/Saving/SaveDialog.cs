using System.IO;
using Microsoft.Win32;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>Выбор файла для сохранения стандартным диалогом Windows «Сохранить как».</summary>
public static class SaveDialog
{
    /// <summary>
    /// Показать диалог <paramref name="title"/> с фильтром <paramref name="filter"/> (одно расширение — оно же
    /// подставляется к имени без расширения) и именем <paramref name="fileName"/> в папке <paramref name="folder"/>. Файл уже
    /// есть — Windows спрашивает о перезаписи. Вызывать из потока окна.
    /// </summary>
    /// <returns>Полный путь выбранного файла или <c>null</c>, если сохранение отменено.</returns>
    public static string? Ask(string title, string filter, string fileName, string? folder)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = fileName,
            DefaultExt = Path.GetExtension(fileName),
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = folder ?? ""
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
