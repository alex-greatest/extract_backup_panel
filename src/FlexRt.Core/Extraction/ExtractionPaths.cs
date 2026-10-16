namespace FlexRt.Extraction;

/// <summary>Входные файлы и выходы одного запуска извлечения.</summary>
/// <param name="FwxPath">Путь к <c>pdata.fwc</c> панели.</param>
/// <param name="PlcPath">Проект ПЛК: файл <c>PEData.plf</c> или папка карты ПЛК с <c>SIMATIC.S7S</c>; может не существовать.</param>
/// <param name="StringsWorkbookPath">Книга строк языков (<c>PDATA.xlsx</c>).</param>
/// <param name="PanelWorkbookPath">Книга данных панели (<c>panel_data.xlsx</c>).</param>
/// <param name="HexDir">Папка hex-файлов таблиц; удаляется перед записью.</param>
/// <param name="PicturesDir">Папка картинок панели (<c>images</c>); удаляется перед записью.</param>
public sealed record ExtractionPaths(string FwxPath, string PlcPath, string StringsWorkbookPath, string PanelWorkbookPath, string HexDir, string PicturesDir);
