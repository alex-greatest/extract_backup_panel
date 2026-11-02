using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Объект из списка экрана до проверки положения: элемент, его вид и имя, рамка, если прочиталась.</summary>
/// <param name="Element">Id таблицы и номер элемента.</param>
/// <param name="Table">Имя таблицы.</param>
/// <param name="Item">Элемент в файле — для смещения в предупреждении.</param>
/// <param name="Kind">Первое слово элемента (3, 1, 0x0b …); элемент короче 2 байт — <c>null</c>.</param>
/// <param name="Name">Имя объекта; нет или не прочитано — пустое.</param>
/// <param name="Bounds">Положение; не прочитано — <c>null</c>.</param>
internal sealed record ObjectCandidate(VarReference Element, string Table, TableItem Item, int? Kind, string Name, ScreenRect? Bounds);
