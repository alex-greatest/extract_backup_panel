using System.Globalization;
using FlexRt.Export.PanelData.Screens;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Export.ScreenEvents;

/// <summary>
/// Тексты вкладки Events окна: события Loaded и Cleared экрана, функции и параметры, как на вкладке Events экрана в
/// TIA (решения пользователя 2026-10-29). Только для окна: в Excel и текст чтения события не идут.
/// </summary>
public sealed class ScreenEventTexts
{
    /// <summary>Имя функции или значение, которое не прочитано.</summary>
    public const string NotRead = "не прочитано";

    /// <summary>Разделитель имён функций с одинаковыми байтами.</summary>
    private const string NameSeparator = " / ";

    /// <summary>Имя объекта проекта по виду (VARIANT тип 0x12; таблицы объектов не разобраны, имён нет).</summary>
    private static readonly Dictionary<long, string> ProjectObjectNames = new()
    {
        [0x44] = "журнал",
        [0xe0] = "отчёт",
        [0x34] = "текстовый список"
    };

    /// <summary>События по номеру записи SCREEN и виду.</summary>
    private readonly Dictionary<(int Screen, ScreenEventKind Kind), ScreenEvent> _events;

    /// <summary>Имена тегов документа по ссылке на запись VAR.</summary>
    private readonly TagNames _tagNames;

    /// <summary>Собрать события и имена тегов документа один раз на панель.</summary>
    public ScreenEventTexts(FwxDocument doc)
    {
        // повтор (экран, событие) не должен ронять открытие панели: берётся первое
        _events = doc.ScreenEvents.DistinctBy(e => (e.ScreenIndex, e.Kind)).ToDictionary(e => (e.ScreenIndex, e.Kind));
        _tagNames = new TagNames(doc);
    }

    /// <summary>События экрана: всегда Loaded и Cleared (события нет — без функций).</summary>
    /// <param name="screen">Запись SCREEN экрана.</param>
    /// <returns>Loaded, затем Cleared.</returns>
    public IReadOnlyList<ScreenEventView> Of(Screen screen) =>
        [.. Enum.GetValues<ScreenEventKind>().Select(kind => View(screen, kind))];

    /// <summary>Одно событие экрана.</summary>
    /// <returns>Событие для окна.</returns>
    private ScreenEventView View(Screen screen, ScreenEventKind kind)
    {
        if (!_events.TryGetValue((screen.Index, kind), out var screenEvent))
        {
            return new ScreenEventView(kind.ToString(), true, []);
        }
        return screenEvent.Functions is null
            ? new ScreenEventView(kind.ToString(), false, [])
            : new ScreenEventView(kind.ToString(), true, [.. screenEvent.Functions.Select((f, i) => Function(f, i + 1))]);
    }

    /// <summary>Функция: имя и параметры; не прочитана — <see cref="NotRead"/> без параметров.</summary>
    /// <returns>Функция для окна.</returns>
    private FunctionView Function(FunctionCall call, int number)
    {
        var element = $"{call.Table} {call.Element.Index + 1}";
        if (call.Arguments is null)
        {
            return new FunctionView(number, NotRead, false, element, []);
        }
        var parameters = call.Arguments.Select((a, k) => new ParameterView(ParameterName(call, k), Value(a))).ToList();
        return new FunctionView(number, FunctionName(call), true, element, parameters);
    }

    /// <summary>Имя функции: имена словаря через « / »; не опознана — <c>FUNC_X (подкод 0x3)</c>.</summary>
    /// <returns>Имя.</returns>
    private static string FunctionName(FunctionCall call) =>
        call.Names.Count > 0 ? string.Join(NameSeparator, call.Names) : $"{call.Table} (подкод 0x{call.Subcode:x})";

    /// <summary>Имя параметра из словаря; не известно — <c>Параметр k</c>.</summary>
    /// <returns>Имя.</returns>
    private static string ParameterName(FunctionCall call, int index) =>
        call.ParameterNames.ElementAtOrDefault(index) is { Length: > 0 } name ? name : $"Параметр {index + 1}";

    /// <summary>Значение параметра текстом.</summary>
    /// <returns>Текст; не известно — <c>?</c>.</returns>
    private string Value(FunctionArgument argument) => argument.Kind switch
    {
        FunctionArgumentKind.Tag => _tagNames.Of(argument.Reference) ?? ScreensTable.Unknown,
        FunctionArgumentKind.Integer or FunctionArgumentKind.UInt32 => argument.Number.ToString(CultureInfo.InvariantCulture),
        FunctionArgumentKind.Real => RealText(argument.Real),
        FunctionArgumentKind.Text or FunctionArgumentKind.ScreenObject => argument.Text,
        FunctionArgumentKind.ProjectObject =>
            $"{ProjectObjectNames.GetValueOrDefault(argument.ObjectKind, $"объект 0x{argument.ObjectKind:x}")} #{argument.Number}",
        FunctionArgumentKind.Reference => $"{argument.Text} {argument.Number + 1}",
        _ => ScreensTable.Unknown
    };

    /// <summary>Число f64: целое — без дробной части, дробное — в инвариантной культуре.</summary>
    /// <returns>Текст числа.</returns>
    private static string RealText(double value) =>
        double.IsInteger(value) && value is >= long.MinValue and <= long.MaxValue
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("R", CultureInfo.InvariantCulture);
}
