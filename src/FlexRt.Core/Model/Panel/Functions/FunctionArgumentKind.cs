namespace FlexRt.Model.Panel.Functions;

/// <summary>Вид значения параметра системной функции.</summary>
public enum FunctionArgumentKind
{
    /// <summary>Тег: ссылка на запись VAR (<see cref="FunctionArgument.Reference"/>).</summary>
    Tag,

    /// <summary>Целое со знаком (VARIANT тип 3): <see cref="FunctionArgument.Number"/>.</summary>
    Integer,

    /// <summary>Целое без знака (VARIANT тип 0x13): <see cref="FunctionArgument.Number"/>.</summary>
    UInt32,

    /// <summary>Число f64 (VARIANT тип 5): <see cref="FunctionArgument.Real"/>.</summary>
    Real,

    /// <summary>Строка (VARIANT тип 8): <see cref="FunctionArgument.Text"/>.</summary>
    Text,

    /// <summary>Объект экрана или соединение (VARIANT тип 3, слово +2 равно 2): имя в <see cref="FunctionArgument.Text"/>, элемент в <see cref="FunctionArgument.Reference"/>.</summary>
    ScreenObject,

    /// <summary>
    /// Объект проекта (VARIANT тип 0x12): номер в <see cref="FunctionArgument.Number"/>, вид в
    /// <see cref="FunctionArgument.ObjectKind"/> (0x44 — журнал, 0xe0 — отчёт, 0x34 — текстовый список).
    /// </summary>
    ProjectObject,

    /// <summary>Ссылка на элемент неразобранной таблицы: имя таблицы в <see cref="FunctionArgument.Text"/>, номер элемента с нуля в <see cref="FunctionArgument.Number"/>.</summary>
    Reference,

    /// <summary>Значение не известно: ссылка на таблицу не из TOC или незнакомый тип VARIANT.</summary>
    Unknown
}
