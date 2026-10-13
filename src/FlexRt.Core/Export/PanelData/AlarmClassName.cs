using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

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
    /// по ID словом, свой — его Display name на первом языке панели (имени своего класса в панели
    /// нет), номер вне списка — <c>?</c>.
    /// </summary>
    /// <returns>Имя стандартного класса, Display name своего или <c>?</c>.</returns>
    public static string Format(IReadOnlyList<AlarmClass> classes, int index, Func<long, string> firstLanguageText)
    {
        if (index >= classes.Count)
        {
            return "?";
        }
        var alarmClass = classes[index];
        return StandardClasses.GetValueOrDefault(alarmClass.Id) ?? firstLanguageText(alarmClass.DisplayNameIdx);
    }
}
