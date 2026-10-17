namespace FlexRt.Ui.Panel.Screens;

/// <summary>
/// Виды экрана — значения колонки Вид листа «Экраны». Повторяют закрытые константы <c>ScreensTable</c> ядра: если ядро
/// выдаст другой вид, окно покажет такой экран в папке «Вид не опознан», а не потеряет его. Окно раскладывает по ним
/// экраны в дереве проекта и выбирает значок.
/// </summary>
public static class ScreenKind
{
    /// <summary>Обычный экран — экран с номером.</summary>
    public const string Screen = "Screen";

    /// <summary>Pop-up экран.</summary>
    public const string PopUp = "Pop-up screen";

    /// <summary>Slide-in экран.</summary>
    public const string SlideIn = "Slide-in screen";

    /// <summary>Permanent area: у неё нет своей папки, в дереве она лежит прямо в Screen management, как в TIA.</summary>
    public const string PermanentArea = "Permanent area";

    /// <summary>Виды, у которых в дереве своё место; остальные (<c>?</c> и любые новые) — в папке «Вид не опознан».</summary>
    public static readonly IReadOnlySet<string> Recognized = new HashSet<string> { Screen, PopUp, SlideIn, PermanentArea };
}
