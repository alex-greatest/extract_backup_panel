using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Класс аларма для колонки Alarm class: общий для дискретных и аналоговых алармов.</summary>
public static class AlarmClassName
{
    /// <summary>
    /// Стандартные классы по ID: сверено с полем ID класса в TIA на A603A0097. Их Display name
    /// бывает пустым, а имени класса в панели нет.
    /// </summary>
    private static readonly Dictionary<int, string> StandardClasses = new()
    {
        [1] = "Errors",
        [2] = "Warnings",
        [3] = "System",
        [4] = "Diagnosis events",
        [33] = "Acknowledgement",
        [34] = "No Acknowledgement"
    };

    /// <summary>
    /// Класс с номером <paramref name="index"/> в списке <paramref name="classes"/>: стандартный —
    /// по ID словом, свой — ячейка его Display name по номеру строки (<paramref name="displayName"/>: в Excel — первый
    /// язык, в окне — выбранный; имени своего класса в панели нет), номер вне списка — <c>?</c>.
    /// </summary>
    /// <returns>Имя стандартного класса, Display name своего или <c>?</c>.</returns>
    public static ReportCell Format(IReadOnlyList<AlarmClass> classes, int index, Func<long, ReportCell> displayName)
    {
        if (index >= classes.Count)
        {
            return "?";
        }
        var alarmClass = classes[index];
        return StandardName(alarmClass.Id) is { } name ? name : displayName(alarmClass.DisplayNameIdx);
    }

    /// <summary>Имя стандартного класса по ID (<c>Errors</c>, <c>Acknowledgement</c>, …).</summary>
    /// <returns>Имя класса; свой класс — <c>null</c>: его имени в панели нет.</returns>
    public static string? StandardName(int id) => StandardClasses.GetValueOrDefault(id);
}
