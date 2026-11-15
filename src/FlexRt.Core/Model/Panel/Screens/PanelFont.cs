namespace FlexRt.Model.Panel.Screens;

/// <summary>Шрифт из элемента FONT, как Text format → Font в TIA.</summary>
/// <param name="Name">Имя шрифта (<c>Tahoma</c>).</param>
/// <param name="Size">Размер, как Font size в TIA (16, 20).</param>
/// <param name="Bold">Жирный: вес 700 и больше.</param>
/// <param name="Italic">Курсив.</param>
/// <param name="Underline">Подчёркнутый.</param>
/// <param name="Strikeout">Зачёркнутый; FONT +0x30, проверено переключением в редакторе TP700 V21.</param>
public sealed record PanelFont(string Name, double Size, bool Bold, bool Italic, bool Underline, bool Strikeout = false);
