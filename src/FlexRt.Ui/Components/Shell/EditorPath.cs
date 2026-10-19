using System.IO;
using FlexRt.Ui.Panel.Pictures;
using FlexRt.Ui.Panel.Screens;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Components.Shell;

/// <summary>Путь выбранного узла в заголовке редактора, как в TIA.</summary>
public static class EditorPath
{
    /// <summary>Разделитель частей пути.</summary>
    private const string Separator = "  ▸  ";

    /// <summary>
    /// Путь узла: <c>pdata.fwc ▸ Screen ▸ Root screen</c> у экрана, <c>pdata.fwc ▸ HMI alarms ▸ Discrete alarms</c> у
    /// раздела, <c>pdata.fwc ▸ Картинки</c> у картинок.
    /// </summary>
    /// <param name="selected">Выбранный узел дерева.</param>
    /// <param name="path">Путь открытого файла.</param>
    /// <returns>Текст строки; ничего не выбрано или файл не задан — пусто.</returns>
    public static string Of(object? selected, string? path)
    {
        var node = selected switch
        {
            ScreenItem item => $"{item.Kind}{Separator}{item.Name}",
            PanelSection section => section.Path,
            PanelPictures => PanelPictures.Title,
            _ => null
        };
        return path is null || node is null ? "" : $"{Path.GetFileName(path)}{Separator}{node}";
    }
}
