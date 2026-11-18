namespace FlexRt.Model.Panel.Screens;

/// <summary>Подписи кнопки из SYMB по управляющему тегу.</summary>
/// <param name="Tag">Прямой тег VAR; другая цепочка или тег отсутствует — null.</param>
/// <param name="Entries">Диапазоны и подписи; неподтверждённая раскладка или ошибка — null.</param>
public sealed record ButtonCaptionList(VarReference? Tag, IReadOnlyList<ButtonListEntry>? Entries);
