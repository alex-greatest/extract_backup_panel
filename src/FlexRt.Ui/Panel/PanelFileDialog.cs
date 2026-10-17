using Microsoft.Win32;

namespace FlexRt.Ui.Panel;

/// <summary>Выбор файла панели <c>pdata.fwc</c> стандартным диалогом Windows.</summary>
public static class PanelFileDialog
{
    /// <summary>Фильтр диалога: скомпилированная конфигурация панели и любые файлы (шаговые копии с другим именем).</summary>
    private const string Filter = "Конфигурация панели (*.fwc)|*.fwc|Все файлы (*.*)|*.*";

    /// <summary>Показать диалог открытия файла. Вызывать из потока окна (так вызываются обработчики компонентов).</summary>
    /// <returns>Полный путь выбранного файла или <c>null</c>, если выбор отменён.</returns>
    public static string? Ask()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Открыть pdata.fwc",
            Filter = Filter,
            CheckFileExists = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
