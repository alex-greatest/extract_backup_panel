namespace FlexRt.Model.Panel.Screens;

/// <summary>Поля текста объекта в пикселях, как Layout → Margins в TIA (байты PS_Text +0x1c…0x1f).</summary>
/// <param name="Top">Сверху.</param>
/// <param name="Bottom">Снизу.</param>
/// <param name="Left">Слева.</param>
/// <param name="Right">Справа.</param>
public sealed record TextMargins(int Top, int Bottom, int Left, int Right);
