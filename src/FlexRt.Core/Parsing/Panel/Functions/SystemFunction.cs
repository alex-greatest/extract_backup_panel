namespace FlexRt.Parsing.Panel.Functions;

/// <summary>Запись словаря системных функций (<c>SystemFunctions.json</c>): какая функция TIA лежит в таблице FUNC_* с подкодом.</summary>
/// <param name="Table">Имя таблицы из TOC (<c>FUNC_BIT</c>).</param>
/// <param name="Subcode">Подкод в hex с <c>0x</c> (<c>0x1</c>).</param>
/// <param name="Names">
/// Имена функции в TIA; два — функции с одинаковыми байтами (HTMLBrowserPageUp и ScreenObjectPageUp), показываются
/// через « / ».
/// </param>
/// <param name="Parameters">Имена параметров в TIA по порядку параметров в элементе; пустое — имени нет.</param>
/// <param name="ObjectReferences">
/// Выбор одного из <paramref name="Names"/> по объекту-параметру: ключ — таблица, на которую ссылается элемент
/// объекта, значение — имя функции (RecipeView — ссылка на RECIPE_FACTORY); <c>null</c> — выбора нет.
/// </param>
/// <param name="FirstReference">
/// Имена и параметры, если первый параметр — ссылка (тег), а не значение: ActivateScreenByNumber на подкоде
/// ActivateScreen (у ActivateScreen первый параметр — всегда экран, у ActivateScreenByNumber — всегда тег, каталог TIA
/// <c>Utah-Functions.en.xml</c>; копия <c>data/real/steps-iofield/</c> 87); <c>null</c> — выбора нет.
/// </param>
internal sealed record SystemFunction(
    string Table,
    string Subcode,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Parameters,
    IReadOnlyDictionary<string, string>? ObjectReferences = null,
    SystemFunctionVariant? FirstReference = null);
