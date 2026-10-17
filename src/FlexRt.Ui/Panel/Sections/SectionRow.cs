namespace FlexRt.Ui.Panel.Sections;

/// <summary>Строка таблицы раздела, выбранная для панели Properties.</summary>
/// <param name="Section">Раздел, в таблице которого строка.</param>
/// <param name="Index">Номер строки в <see cref="PanelSection.Table"/> с нуля (порядок листа, не сортировки).</param>
public sealed record SectionRow(PanelSection Section, int Index);
