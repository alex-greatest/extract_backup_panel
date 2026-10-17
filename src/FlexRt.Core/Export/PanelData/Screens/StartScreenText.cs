using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Screens;

/// <summary>Текст стартового экрана для «Сводки», текста запуска и лога.</summary>
public static class StartScreenText
{
    /// <summary>Текст, если стартовый экран не известен.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>
    /// Имя и номер стартового экрана: <c>Root screen (1)</c>. Номера нет (нет GUI или разбор оборвался) —
    /// <c>неизвестно</c>; номер есть, но экрана с ним нет среди записей SCREEN — <c>неизвестно (5)</c>.
    /// </summary>
    /// <returns>Текст значения.</returns>
    public static string Format(FwxDocument doc)
    {
        if (doc.StartScreenNumber is not { } number)
        {
            return Unknown;
        }
        var name = ScreensTable.Screens(doc)
            .Where(s => s.Number == number)
            .Select(s => s.Screen.Name)
            .FirstOrDefault();
        return $"{name ?? Unknown} ({number})";
    }
}
