using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Привязка аппаратных клавиш SCREEN к кнопкам конкретного экрана.</summary>
internal static class ButtonDirectKeyReader
{
    /// <summary>Сопоставить записи по рамке на экране, где они объявлены.</summary>
    /// <returns>Назначения по элементу кнопки и номеру записи SCREEN.</returns>
    public static ILookup<VarReference, (int Screen, FunctionCall Call)> Read(FwxDocument doc, IReadOnlyList<ScreenObject> buttons)
    {
        var byScreen = buttons.ToLookup(button => button.ScreenIndex);
        var found = new List<(VarReference Button, int Screen, FunctionCall Call)>();
        if (doc.FindTable("SCREEN") is { } table)
        {
            var overview = doc.Screens.FirstOrDefault(screen => screen is { Index: 0, IsService: false });
            var numbered = doc.ScreenNumbers.Select(entry => entry.ScreenIndex).ToHashSet();
            var popups = doc.Screens.Where(screen => screen.IsService).SelectMany(screen => screen.Objects)
                .Where(link => link.Table == table.Id).Select(link => link.Index).ToHashSet();
            var permanent = doc.PermanentAreaHeight.GetValueOrDefault();
            foreach (var screen in doc.Screens.Where(screen => screen.DirectKeys.Count > 0))
            {
                var offset = Offset(screen, overview, numbered, popups, permanent);
                var objects = byScreen[screen.Index].DistinctBy(button => button.Element)
                    .ToLookup(button => button.Bounds with { Top = button.Bounds.Top + offset });
                foreach (var (bounds, bit) in screen.DirectKeys)
                {
                    if (objects[bounds].Take(2).ToList() is [var button])
                    {
                        var call = new FunctionCall(new VarReference(table.Id, screen.Index), table.Name, null,
                            ["DirectKey"], ["Bit"], [new FunctionArgument(FunctionArgumentKind.Integer, Number: bit)]);
                        found.Add((button.Element, screen.Index, call));
                    }
                }
            }
        }
        return found.ToLookup(entry => entry.Button, entry => (entry.Screen, entry.Call));
    }

    /// <summary>Сдвиг координат DirectKey для обычного экрана; при неизвестном виде проверяется размер рабочей области.</summary>
    /// <returns>Высота Permanent area или ноль для экрана без полосы.</returns>
    private static int Offset(Screen screen, Screen? overview, HashSet<int> numbered, HashSet<int> popups, int permanent)
    {
        if (numbered.Contains(screen.Index))
        {
            return permanent;
        }
        return overview is not null && screen.Index != overview.Index && !popups.Contains(screen.Index)
            && screen.Name is not ("Slide-in screen top" or "Slide-in screen left" or "Slide-in screen bottom" or "Slide-in screen right")
            && screen.Width == overview.Width && screen.Height + permanent == overview.Height ? permanent : 0;
    }
}
