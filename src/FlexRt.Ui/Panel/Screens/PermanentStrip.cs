namespace FlexRt.Ui.Panel.Screens;

/// <summary>Полоса Permanent area над обычным экраном на холсте, как в редакторе экранов TIA.</summary>
/// <param name="Height">Высота Permanent area в пикселях из VIEW_DYN, больше нуля.</param>
/// <param name="BackgroundRgb">Цвет фона Permanent area (запись <c>Overview</c> SCREEN); <c>null</c> — не прочитан.</param>
/// <param name="PanelHeight">Высота панели — высота записи <c>Overview</c>; <c>null</c> — записи нет.</param>
public sealed record PermanentStrip(int Height, int? BackgroundRgb, int? PanelHeight);
