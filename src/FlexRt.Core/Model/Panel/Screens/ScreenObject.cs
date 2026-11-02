namespace FlexRt.Model.Panel.Screens;

/// <summary>
/// Объект экрана с положением: элемент таблицы VIEW_* из списка объектов записи SCREEN (через обёртки VIEW_DARK и
/// VIEW_MOVE — объект внутри них). Объекты шаблона экрана входят в список каждого экрана с этим шаблоном.
/// </summary>
/// <param name="ScreenIndex">Номер записи SCREEN экрана, с нуля.</param>
/// <param name="Element">Элемент объекта: Id таблицы и номер элемента.</param>
/// <param name="Table">Имя таблицы объекта (<c>VIEW_BUTTONS</c>).</param>
/// <param name="Name">Имя объекта, как в TIA (<c>Exit</c>); у объекта без имени — пустое.</param>
/// <param name="Bounds">Положение и размер на экране.</param>
public sealed record ScreenObject(int ScreenIndex, VarReference Element, string Table, string Name, ScreenRect Bounds);
