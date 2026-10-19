namespace FlexRt.Export.PanelData;

/// <summary>Итог построения одного листа книги (<see cref="PanelDataBook.TryBuild(PanelDataSource)"/>).</summary>
/// <param name="Sheet">Построенный лист; сбой — пустой лист с именем и раскладкой из описания.</param>
/// <param name="Error">Исключение условия наличия или построителя листа; построен — <c>null</c>.</param>
public sealed record BuiltSheet(ReportSheet Sheet, Exception? Error);
