using System.Globalization;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Export.ScreenObjects;

/// <summary>Тексты настроек анимаций I/O field для окна.</summary>
public static class FieldAnimationTexts
{
    /// <summary>Название условия в дереве TIA.</summary>
    /// <returns>Visibility либо Control enable.</returns>
    public static string Name(FieldCondition condition) => condition.Kind == FieldConditionKind.Visibility ? "Visibility" : "Control enable";

    /// <summary>Имя движения; совпадающие данные Horizontal/Vertical и вырожденного Diagonal не различаются по файлу.</summary>
    /// <returns>Подтверждённый вид или все возможные виды без угадывания исходного выбора.</returns>
    public static string Name(FieldMovement movement) => movement switch
    {
        { Direct: true } => "Direct movement",
        { IsRead: false } => "Movement",
        _ when movement.StartX != movement.EndX && movement.StartY != movement.EndY => "Diagonal movement",
        _ when movement.StartX != movement.EndX => "Horizontal / Diagonal movement",
        _ when movement.StartY != movement.EndY => "Vertical / Diagonal movement",
        _ => "Movement"
    };

    /// <summary>Числовое значение анимации без зависимости от культуры Windows.</summary>
    /// <returns>Число либо знак неизвестного значения.</returns>
    public static ReportCell Value(double? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "?";

    /// <summary>Действие условия в диапазоне или при установленном бите.</summary>
    /// <returns>Visible/Invisible, Enabled/Disabled либо сообщение о непрочитанном состоянии.</returns>
    public static string State(FieldCondition condition) => (condition.Kind, condition.Active) switch
    {
        (FieldConditionKind.Visibility, true) => "Visible",
        (FieldConditionKind.Visibility, false) => "Invisible",
        (_, true) => "Enabled",
        (_, false) => "Disabled",
        _ => "не прочитано"
    };
}
