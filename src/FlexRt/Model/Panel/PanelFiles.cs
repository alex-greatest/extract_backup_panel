namespace FlexRt.Model.Panel;

/// <summary>Сведения о панели из файлов рядом с <c>pdata.fwc</c>.</summary>
/// <param name="Model">Модель панели из <c>ProjectCharacteristics.rdf</c> (<c>TP1500 Comfort V2</c>); <c>null</c>, если файла нет.</param>
/// <param name="RuntimeVersion">Версия Runtime из <c>BuildInfo.txt</c> бэкапа панели (<c>17.00.00.07</c>); <c>null</c>, если файла нет.</param>
public sealed record PanelFiles(string? Model, string? RuntimeVersion);
