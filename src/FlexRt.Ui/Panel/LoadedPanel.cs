using FlexRt.Extraction;
using FlexRt.Model.Panel;
using FlexRt.Ui.Panel.Pictures;
using FlexRt.Ui.Panel.Screens;
using FlexRt.Ui.Panel.Sections;

namespace FlexRt.Ui.Panel;

/// <summary>
/// Итог открытия панели в окне (<see cref="PanelLoader.LoadAsync"/>): при успехе — экраны, картинки, разделы, их число и строки
/// Info; при сбое — только строки Info и текст ошибки для строки состояния.
/// </summary>
/// <param name="Screens">Экраны панели в порядке листа «Экраны»; при сбое — пусто.</param>
/// <param name="Pictures">Картинки панели для узла «Картинки»; при сбое — <see cref="PanelPictures.None"/>.</param>
/// <param name="View">Разделы панели; при сбое — <c>null</c>.</param>
/// <param name="Strings">Строки языков панели — для сохранения <c>PDATA.xlsx</c>; при сбое — пусто.</param>
/// <param name="ScreenCount">Число обычных экранов (вида Screen) — как строка «Экранов» на «Сводке»; при сбое — 0.</param>
/// <param name="Lines">Строки вкладки Info: текст чтения панели и данных ПЛК, затем предупреждения о непостроенных
/// разделах или строка ошибки.</param>
/// <param name="Error">Текст ошибки для строки состояния; <c>null</c> — панель открыта.</param>
public sealed record LoadedPanel(
    IReadOnlyList<ScreenItem> Screens,
    PanelPictures Pictures,
    PanelView? View,
    IReadOnlyList<LangString> Strings,
    int ScreenCount,
    IReadOnlyList<TranscriptLine> Lines,
    string? Error);
