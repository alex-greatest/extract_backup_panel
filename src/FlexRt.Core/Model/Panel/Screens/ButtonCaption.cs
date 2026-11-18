namespace FlexRt.Model.Panel.Screens;

/// <summary>Подпись состояния кнопки: текст, картинка или оба значения.</summary>
/// <param name="TextIndex">Номер строки STRINGSTORE с единицы; нет — null.</param>
/// <param name="Picture">Ссылка на элемент картинки; нет — null.</param>
public sealed record ButtonCaption(int? TextIndex, VarReference? Picture);
