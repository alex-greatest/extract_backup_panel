using System.Globalization;
using FlexRt.Export.PanelData.Tags;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Export.ScreenObjects;

/// <summary>
/// Тексты свойств I/O field для панели свойств и холста окна — как в TIA (<c>Input/output</c>, <c>Decimal</c>): сюда же —
/// сборка Format pattern, которого в файле нет (проверяет тест на шаговых копиях).
/// </summary>
public static class IoFieldTexts
{
    /// <summary>Код формата String (<see cref="IoField.DisplayFormat"/>).</summary>
    private const int StringFormat = 0;

    /// <summary>Код формата Decimal.</summary>
    private const int Decimal = 1;

    /// <summary>Код формата Hexadecimal.</summary>
    private const int Hexadecimal = 2;

    /// <summary>Код формата Binary.</summary>
    private const int Binary = 3;

    /// <summary>Код формата Date (шаг 37).</summary>
    private const int DateFormat = 4;

    /// <summary>Код формата Time (шаг 39).</summary>
    private const int TimeFormat = 5;

    /// <summary>Код формата Date/time (шаг 38).</summary>
    private const int DateTimeFormat = 6;

    /// <summary>Код стиля рамки: одинарная линия — Solid, а также Dash, Dot, DashDot, DashDotDot (шаги 41–44).</summary>
    private const int SingleLine = 1;

    /// <summary>Код стиля рамки Double line.</summary>
    private const int DoubleLine = 2;

    /// <summary>Код выравнивания: по горизонтали Right, по вертикали Top (шаги 25, 26).</summary>
    private const int AlignStart = 0;

    /// <summary>Код выравнивания: Center и Middle (шаг 24 и база).</summary>
    private const int AlignCenter = 1;

    /// <summary>Код выравнивания: по горизонтали Left, по вертикали Bottom (база, шаг 27).</summary>
    private const int AlignEnd = 2;

    /// <summary>Режим, как Type → Mode в TIA.</summary>
    /// <returns><c>Input/output</c>, <c>Input</c> или <c>Output</c>.</returns>
    public static string Mode(IoFieldMode mode) => mode switch
    {
        IoFieldMode.InputOutput => "Input/output",
        IoFieldMode.Input => "Input",
        _ => "Output"
    };

    /// <summary>Формат, как Display format в TIA.</summary>
    /// <returns>Имя формата; код не известен — <c>? (код N)</c>.</returns>
    public static string Format(int code) => code switch
    {
        StringFormat => "String",
        Decimal => "Decimal",
        Hexadecimal => "Hexadecimal",
        Binary => "Binary",
        DateFormat => "Date",
        TimeFormat => "Time",
        DateTimeFormat => "Date/time",
        _ => Unknown(code)
    };

    /// <summary>
    /// Format pattern, собранный из длины, знаков после запятой и знака (сам текст в файле не хранится): Decimal —
    /// <c>s</c> и девятки, у тега Real и LReal — с точкой и девятками после неё (<c>s99999.99</c>), у целого — без точки
    /// (знаки после запятой — сдвиг, Decimal places); Hexadecimal — <c>F</c>, Binary — <c>1</c>, String — <c>?</c> на
    /// символ; у Date, Time и Date/time шаблона нет (в TIA поле недоступно). Сверено с TIA: шаги 07–14 Project1 и все 105
    /// полей A603A0097.
    /// </summary>
    /// <returns>Шаблон; у формата без шаблона — пусто.</returns>
    public static string Pattern(IoField field) => field.DisplayFormat switch
    {
        Decimal => Number(field, "s", '9'),
        Hexadecimal => new string('F', field.PatternLength),
        Binary => new string('1', field.PatternLength),
        StringFormat => new string('?', field.PatternLength),
        _ => ""
    };

    /// <summary>
    /// Decimal places, как в TIA: у Decimal с целым тегом — сдвиг запятой, с Real и LReal — 0; тип тега не известен, а
    /// знаков после запятой больше нуля — <c>?</c>; у других форматов — пусто (в TIA поле недоступно).
    /// </summary>
    /// <returns>Текст поля.</returns>
    public static string DecimalPlaces(IoField field) => field.DisplayFormat != Decimal
        ? ""
        : field.TagType is null && field.Decimals > 0 ? "?"
        : IsFloat(field) ? "0" : field.Decimals.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Decimal places доступно в TIA: формат Decimal и целый тег (у Real и LReal и тега неизвестного типа — серое, как на
    /// снимках TIA пользователя; у целого тега — не сверено по снимку).
    /// </summary>
    /// <returns><c>true</c> — поле показывается доступным.</returns>
    public static bool DecimalPlacesEnabled(IoField field) => field.DisplayFormat == Decimal && !IsFloat(field);

    /// <summary>Field length доступно в TIA: у Decimal поле серое (снимки TIA пользователя).</summary>
    /// <returns><c>true</c> — поле показывается доступным.</returns>
    public static bool FieldLengthEnabled(IoField field) => field.DisplayFormat != Decimal;

    /// <summary>
    /// Field length, как в TIA: у Decimal в файл не попадает (шаг 40) — пусто; у остальных форматов — длина из файла
    /// (у Date и Time 20, у Date/time 40, у Hexadecimal, Binary и String — длина шаблона).
    /// </summary>
    /// <returns>Текст поля.</returns>
    public static string FieldLength(IoField field) =>
        field.DisplayFormat == Decimal ? "" : field.PatternLength.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Текст в поле на холсте — как в редакторе TIA, значения тега в файле нет: шаблон нулями (<c>0000000</c>,
    /// <c>+00000.00</c>; знак <c>+</c> — догадка), у String — пусто; Units — через пробел.
    /// </summary>
    /// <returns>Текст поля.</returns>
    public static string Sample(IoField field)
    {
        var text = field.DisplayFormat switch
        {
            Decimal => Number(field, "+", '0'),
            Hexadecimal or Binary => new string('0', field.PatternLength),
            _ => ""
        };
        return $"{text} {field.Unit}".Trim();
    }

    /// <summary>
    /// Стиль рамки, как Border → Style в TIA. Код 1 — одинарная линия: Dash, Dot, DashDot и DashDotDot компилируются в
    /// тот же код (шаги 41–44, файл совпал с Solid), поэтому показывается <c>Solid line</c>.
    /// </summary>
    /// <returns><c>Solid line</c>, <c>Double line</c>; код не известен — <c>? (код N)</c>.</returns>
    public static string EdgeStyle(int code) => code switch
    {
        SingleLine => "Solid line",
        DoubleLine => "Double line",
        _ => Unknown(code)
    };

    /// <summary>Выравнивание по горизонтали, как Text format → Alignment в TIA.</summary>
    /// <returns><c>Left</c>, <c>Center</c>, <c>Right</c>; код не известен — <c>? (код N)</c>.</returns>
    public static string Horizontal(int code) => code switch
    {
        AlignStart => "Right",
        AlignCenter => "Center",
        AlignEnd => "Left",
        _ => Unknown(code)
    };

    /// <summary>Выравнивание по вертикали, как Text format → Alignment в TIA.</summary>
    /// <returns><c>Top</c>, <c>Middle</c>, <c>Bottom</c>; код не известен — <c>? (код N)</c>.</returns>
    public static string Vertical(int code) => code switch
    {
        AlignStart => "Top",
        AlignCenter => "Middle",
        AlignEnd => "Bottom",
        _ => Unknown(code)
    };

    /// <summary>Шрифт, как Text format → Font в TIA: <c>Tahoma, 16px, style=Bold</c>.</summary>
    /// <returns>Имя, размер и начертание.</returns>
    public static string Font(PanelFont font)
    {
        var styles = new List<string>();
        if (font.Bold)
        {
            styles.Add("Bold");
        }
        if (font.Italic)
        {
            styles.Add("Italic");
        }
        if (font.Underline)
        {
            styles.Add("Underline");
        }
        var text = $"{font.Name}, {font.Size.ToString(CultureInfo.InvariantCulture)}px";
        return styles.Count == 0 ? text : $"{text}, style={string.Join(", ", styles)}";
    }

    /// <summary>
    /// Тег — Real или LReal (<see cref="TagTypeNames.IsFloat"/>): знаки после запятой — в шаблоне; тип не известен — тоже
    /// (как у Real).
    /// </summary>
    /// <returns><c>true</c> — дробная часть в шаблоне с точкой.</returns>
    private static bool IsFloat(IoField field) => field.TagType is not { } type || TagTypeNames.IsFloat(type);

    /// <summary>
    /// Десятичное число по шаблону: знак, цифры, у Real (<see cref="IsFloat"/>) — точка и цифры после неё; у целого
    /// тега точки нет (знаки после запятой — сдвиг), все символы шаблона после знака — цифры.
    /// </summary>
    /// <returns>Текст; длина шаблона меньше знака и дробной части — только они.</returns>
    private static string Number(IoField field, string sign, char digit)
    {
        var decimals = IsFloat(field) ? field.Decimals : 0;
        var fraction = decimals > 0 ? decimals + 1 : 0;
        var whole = Math.Max(field.PatternLength - fraction - (field.Signed ? 1 : 0), 0);
        var text = (field.Signed ? sign : "") + new string(digit, whole);
        return decimals > 0 ? $"{text}.{new string(digit, decimals)}" : text;
    }

    /// <summary>Значение кода, которого программа не знает.</summary>
    /// <returns><c>? (код N)</c>.</returns>
    private static string Unknown(int code) => $"? (код {code})";
}
