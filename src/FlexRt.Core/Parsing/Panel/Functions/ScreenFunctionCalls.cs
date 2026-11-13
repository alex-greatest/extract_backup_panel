using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>
/// Параметры функций экранных объектов в порядке TIA, сверенные с XML шага 88 (V21, TP700 Comfort).
/// Исходные параметры FWC включают внутренние объекты и размеры pop-up в конце списка.
/// </summary>
internal static class ScreenFunctionCalls
{
    /// <summary>Минимальная ширина и высота ShowPopupScreenSizable в установленном каталоге TIA V21.</summary>
    private const long MinimumPopupSize = 100;

    /// <summary>Имена параметров ShowPopupScreenSizable в порядке XML TIA.</summary>
    private static readonly string[] SizableParameters =
        ["Name of the screen", "X coordinate", "Y coordinate", "Width", "Height", "Display mode", "Animation", "Animation speed"];

    /// <summary>
    /// У известных функций убрать внутренние параметры и восстановить порядок TIA; другую раскладку оставить как есть.
    /// </summary>
    /// <returns>Функция для отображения; неизвестная раскладка — исходная функция.</returns>
    /// <exception cref="Binary.FwxFormatException">Ссылка pop-up ведёт за пределы таблицы.</exception>
    public static FunctionCall Project(FwxDocument doc, FunctionCall call)
    {
        if (call.Arguments is not { } arguments)
        {
            return call;
        }
        return (call.Table, call.Subcode) switch
        {
            ("FUNC_PRODIAG", 0) when arguments is [_, _, { Kind: FunctionArgumentKind.Integer, Number: 0 }] =>
                call with { Arguments = [arguments[0], arguments[1]] },
            ("FUNC_SYSDIAG", 0) when arguments is [{ Reference: { } reference }, { Kind: FunctionArgumentKind.ScreenObject, Reference: { } view }]
                && doc.FindTable(reference.Table)?.Name == "SCREEN_SYS"
                && doc.FindTable(view.Table)?.Name == "VIEW_OCX" =>
                call with { Arguments = [arguments[0]] },
            ("FUNC_SHOWPOPUPS", 0) when arguments.Count == 8 => Popup(doc, call, arguments),
            ("FUNC_SHOWSLIDEI", 0) when arguments is [{ Kind: FunctionArgumentKind.UInt32 }, _] =>
                SlideIn(doc, call, arguments),
            _ => call
        };
    }

    /// <summary>
    /// Pop-up: номер SCREEN_SYS, X, Y, режим, анимация, скорость, ширина, высота.
    /// Размеры 0/0 — ShowPopupScreen; оба не меньше 100 — ShowPopupScreenSizable (шаг 88 и ограничения каталога TIA).
    /// </summary>
    /// <returns>Функция с параметрами TIA; размеры неизвестны или вне подтверждённой раскладки — исходная функция.</returns>
    private static FunctionCall Popup(FwxDocument doc, FunctionCall call, IReadOnlyList<FunctionArgument> arguments)
    {
        if (arguments[6].Kind != FunctionArgumentKind.Integer || arguments[7].Kind != FunctionArgumentKind.Integer)
        {
            return call;
        }
        var screen = PopupScreenReference.Resolve(doc, arguments[0]);
        var x = Coordinate(arguments[1]);
        var y = Coordinate(arguments[2]);
        if (arguments[6].Number == 0 && arguments[7].Number == 0)
        {
            return call with
            {
                Names = ["ShowPopupScreen"],
                Arguments = [screen, x, y, arguments[3], arguments[4], arguments[5]]
            };
        }
        return arguments[6].Number >= MinimumPopupSize && arguments[7].Number >= MinimumPopupSize
            ? call with
            {
                Names = ["ShowPopupScreenSizable"],
                ParameterNames = SizableParameters,
                Arguments = [screen, x, y, arguments[6], arguments[7], arguments[3], arguments[4], arguments[5]]
            }
            : call;
    }

    /// <summary>Координата pop-up: VARIANT 0x12 со словом 0 содержит число, а не объект проекта (X/Y шага 88).</summary>
    /// <returns>Целое значение координаты или исходный параметр-ссылка на тег.</returns>
    private static FunctionArgument Coordinate(FunctionArgument argument) =>
        argument is { Kind: FunctionArgumentKind.ProjectObject, ObjectKind: 0 }
            ? new FunctionArgument(FunctionArgumentKind.Integer, Number: argument.Number)
            : argument;

    /// <summary>Направления ShowSlideInScreen: 0 left, 1 top, 2 right, 3 bottom (шаги 88–92, V21, TP700 Comfort).</summary>
    /// <returns>Имя экрана из SCREEN; код неизвестен или экран не найден — исходное числовое значение.</returns>
    private static FunctionCall SlideIn(FwxDocument doc, FunctionCall call, IReadOnlyList<FunctionArgument> arguments)
    {
        var name = arguments[0].Number switch
        {
            0 => "Slide-in screen left",
            1 => "Slide-in screen top",
            2 => "Slide-in screen right",
            3 => "Slide-in screen bottom",
            _ => null
        };
        if (name is null)
        {
            return call;
        }
        var screen = doc.Screens.FirstOrDefault(s => !s.IsService && s.Name == name);
        return screen is null ? call : call with
        {
            Arguments = [new FunctionArgument(FunctionArgumentKind.ScreenObject, Text: screen.Name), arguments[1]]
        };
    }
}
