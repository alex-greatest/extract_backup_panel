namespace FlexRt.Ui.Components.Inspector;

/// <summary>Вид поля свойства объекта, как в панели свойств TIA: только вид, значение показывается одинаково.</summary>
public enum ObjectRowControl
{
    /// <summary>Простое поле.</summary>
    Text,

    /// <summary>Список: справа кнопка со стрелкой вниз.</summary>
    Combo,

    /// <summary>Число со стрелками вверх и вниз.</summary>
    Spin
}
