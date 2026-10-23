namespace FlexRt.Ui.Panel;

/// <summary>Цвет из файла панели (<c>R &lt;&lt; 16 | G &lt;&lt; 8 | B</c>) для CSS окна.</summary>
public static class CssColor
{
    /// <summary>Цвет в виде <c>#rrggbb</c>: квадратики цвета в таблицах и свойствах, заливка холста.</summary>
    /// <returns>Текст цвета, например <c>#b6b6b6</c>.</returns>
    public static string Hex(int rgb) => $"#{rgb:x6}";
}
