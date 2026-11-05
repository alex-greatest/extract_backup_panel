namespace FlexRt.Model.Panel.Screens;

/// <summary>
/// I/O field: элемент VIEW_IN (Input, Input/output) или VIEW_OUT (Output) со свойствами General, Appearance и Text
/// format TIA. Положение и имя — у объекта экрана с тем же элементом (<see cref="ScreenObject"/>).
/// </summary>
/// <param name="Element">Элемент объекта: Id таблицы и номер элемента.</param>
/// <param name="Mode">Режим (Type → Mode).</param>
/// <param name="Tag">
/// Имя тега (Process → Tag): элемент VAR прямой ссылки или VAR / VARLIST через ADAPTVALUECHANG; не найден — <c>null</c>.
/// </param>
/// <param name="TagVia">
/// Тег не найден, а ссылки есть: таблица и номер элемента первой ссылки с единицы (<c>RECVAR 4</c>) — тег через таблицу,
/// которая не разобрана; иначе — <c>null</c>.
/// </param>
/// <param name="TagType">Код типа записи VAR тега (<see cref="HmiTag.TypeCode"/>); тег из VARLIST или не найден — <c>null</c>.</param>
/// <param name="DisplayFormat">
/// Код формата (Display format): 0 — String, 1 — Decimal, 2 — Hexadecimal, 3 — Binary, 4 — Date, 5 — Time, 6 — Date/time.
/// </param>
/// <param name="Decimals">Знаков после запятой: у Real — из шаблона, у целых — сдвиг запятой (одно поле в файле).</param>
/// <param name="PatternLength">
/// Длина шаблона Format pattern в символах, вместе со знаком <c>s</c> и точкой; у Date, Time и Date/time — Field length
/// (20 или 40). Field length у Decimal в файл не попадает.
/// </param>
/// <param name="Signed">В шаблоне есть знак (<c>s</c> в начале).</param>
/// <param name="LeadingZeros">Leading zeros.</param>
/// <param name="HiddenInput">Hidden input (Characteristics); у Output в файле нет — <c>false</c>.</param>
/// <param name="Unit">Единицы (Appearance → Text → Unit); нет — пустая строка.</param>
/// <param name="Border">Рамка и углы.</param>
/// <param name="Text">Цвет и выравнивание текста.</param>
/// <param name="Fill">Цвет фона.</param>
/// <param name="Font">Шрифт.</param>
/// <param name="AllowOperatorControl">Allow operator control; установленный бит отключения стиля означает false.</param>
/// <param name="Authorization">Номер права с нуля; ссылки SECURITY_MONITO нет — null.</param>
/// <param name="Limits">Цвета Limits; в файле нет (у тега не задан Range) — <c>null</c>.</param>
public sealed record IoField(
    VarReference Element,
    IoFieldMode Mode,
    string? Tag,
    string? TagVia,
    int? TagType,
    int DisplayFormat,
    int Decimals,
    int PatternLength,
    bool Signed,
    bool LeadingZeros,
    bool HiddenInput,
    string Unit,
    BorderStyle Border,
    TextStyle Text,
    FillStyle Fill,
    PanelFont Font,
    LimitColors? Limits,
    bool AllowOperatorControl = true,
    long? Authorization = null);
