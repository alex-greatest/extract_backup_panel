namespace FlexRt.Model.Panel.Functions;

/// <summary>Значение параметра системной функции; какие поля заполнены — по <see cref="Kind"/>.</summary>
/// <param name="Kind">Вид значения.</param>
/// <param name="Text">Строка, имя объекта или имя таблицы ссылки.</param>
/// <param name="Number">Целое, номер объекта проекта или номер элемента ссылки с нуля.</param>
/// <param name="Real">Число f64.</param>
/// <param name="Reference">
/// Ссылка (Id таблицы и номер элемента с нуля): у тега — на запись VAR, у объекта — на его элемент.
/// </param>
/// <param name="ObjectKind">Вид объекта проекта (u32 на +0x0c блока VARIANT).</param>
public sealed record FunctionArgument(
    FunctionArgumentKind Kind,
    string Text = "",
    long Number = 0,
    double Real = 0,
    VarReference? Reference = null,
    long ObjectKind = 0);
