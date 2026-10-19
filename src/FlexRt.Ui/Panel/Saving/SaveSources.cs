using FlexRt.Model.Panel;
using FlexRt.Ui.Panel.Pictures;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>Что показано в окне и может быть сохранено (<see cref="PanelSaver.SaveAsync"/>).</summary>
/// <param name="View">Разделы открытой панели.</param>
/// <param name="Selected">Выбранный узел дерева: для «Текущего листа» — раздел.</param>
/// <param name="Strings">Строки языков панели — для <c>PDATA.xlsx</c>.</param>
/// <param name="Pictures">Картинки панели и выбранная в галерее.</param>
public sealed record SaveSources(PanelView View, object? Selected, IReadOnlyList<LangString> Strings, PictureSelection Pictures);
