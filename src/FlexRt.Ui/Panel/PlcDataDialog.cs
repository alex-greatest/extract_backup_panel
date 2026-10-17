using Microsoft.Win32;

namespace FlexRt.Ui.Panel;

/// <summary>Выбор данных ПЛК стандартными диалогами Windows: файл <c>PEData.plf</c> или папка копии карты ПЛК.</summary>
public static class PlcDataDialog
{
    /// <summary>Фильтр диалога файла: данные ПЛК проекта TIA и любые файлы (копии с другим именем).</summary>
    private const string Filter = "Данные ПЛК (*.plf)|*.plf|Все файлы (*.*)|*.*";

    /// <summary>Показать диалог выбора файла <c>PEData.plf</c>. Вызывать из потока окна.</summary>
    /// <returns>Полный путь выбранного файла или <c>null</c>, если выбор отменён.</returns>
    public static string? AskFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Данные ПЛК: PEData.plf",
            Filter = Filter,
            CheckFileExists = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>Показать диалог выбора папки копии карты ПЛК (в ней <c>SIMATIC.S7S</c>). Вызывать из потока окна.</summary>
    /// <returns>Полный путь выбранной папки или <c>null</c>, если выбор отменён.</returns>
    public static string? AskCardFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Данные ПЛК: папка карты ПЛК (с SIMATIC.S7S)"
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
