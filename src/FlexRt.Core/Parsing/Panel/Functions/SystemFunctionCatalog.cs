using System.Globalization;
using System.Text.Json;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>
/// Словарь системных функций: ресурс сборки <c>SystemFunctions.json</c>, собран по копиям
/// <c>data/real/steps-events/</c> 09–23 (Project1, V21, TP700 Comfort; имена — как в TIA и Openness). Ключ — имя
/// таблицы из TOC и подкод, а не номер таблицы (номера в разных панелях разные).
/// </summary>
internal static class SystemFunctionCatalog
{
    /// <summary>Имя ресурса словаря в сборке.</summary>
    private const string ResourceName = "FlexRt.SystemFunctions.json";

    /// <summary>Префикс подкода в словаре.</summary>
    private const string HexPrefix = "0x";

    /// <summary>Чтение словаря: имена полей camelCase, неизвестное или повторённое поле и пропущенное обязательное — ошибка.</summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    /// <summary>Функции словаря по таблице и подкоду; загружаются из ресурса при первом обращении к классу.</summary>
    private static readonly Dictionary<(string Table, long Subcode), SystemFunction> Functions = Parse(LoadResource());

    /// <summary>Число записей словаря.</summary>
    public static int Count => Functions.Count;

    /// <summary>Функция по таблице и подкоду.</summary>
    /// <returns>Запись словаря; функции нет — <c>null</c>.</returns>
    public static SystemFunction? Find(string table, long subcode) => Functions.GetValueOrDefault((table, subcode));

    /// <summary>Есть ли в словаре функции таблицы (функции не только в FUNC_*: COPYARCHIVES, WEB_FUNC).</summary>
    /// <returns><c>true</c> — таблица функций.</returns>
    public static bool HasTable(string table) => Functions.Keys.Any(k => k.Table == table);

    /// <summary>Разобрать текст словаря и проверить записи.</summary>
    /// <param name="json">Массив записей <see cref="SystemFunction"/>.</param>
    /// <returns>Записи по таблице и подкоду.</returns>
    /// <exception cref="InvalidOperationException">
    /// Пара «таблица + подкод» повторяется, подкод не <c>0x…</c>, у записи нет имён или выбор по объекту ведёт не на её
    /// имя: ошибка словаря, а не входного файла.
    /// </exception>
    /// <exception cref="JsonException">Текст не JSON словаря: неизвестное, повторённое или пропущенное поле.</exception>
    internal static Dictionary<(string Table, long Subcode), SystemFunction> Parse(string json)
    {
        var entries = JsonSerializer.Deserialize<List<SystemFunction>>(json, Options)
            ?? throw new InvalidOperationException("словарь функций: пусто");
        var functions = new Dictionary<(string, long), SystemFunction>();
        foreach (var entry in entries)
        {
            var key = (entry.Table, Subcode(entry));
            if (entry.Names.Count == 0 || entry.ObjectReferences?.Values.Any(n => !entry.Names.Contains(n)) == true)
            {
                throw new InvalidOperationException($"словарь функций: {entry.Table} {entry.Subcode}: имена не сходятся");
            }
            if (!functions.TryAdd(key, entry))
            {
                throw new InvalidOperationException($"словарь функций: {entry.Table} {entry.Subcode} повторяется");
            }
        }
        return functions;
    }

    /// <summary>Подкод записи числом.</summary>
    /// <returns>Подкод.</returns>
    /// <exception cref="InvalidOperationException">Подкод не hex с префиксом <c>0x</c>.</exception>
    private static long Subcode(SystemFunction entry) =>
        entry.Subcode.StartsWith(HexPrefix, StringComparison.Ordinal)
        && long.TryParse(entry.Subcode.AsSpan(HexPrefix.Length), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code)
            ? code
            : throw new InvalidOperationException($"словарь функций: {entry.Table}: подкод {entry.Subcode} не 0x…");

    /// <summary>Прочитать ресурс словаря.</summary>
    /// <returns>Текст JSON.</returns>
    /// <exception cref="InvalidOperationException">Ресурса нет в сборке: ошибка сборки, а не входного файла.</exception>
    private static string LoadResource()
    {
        using var stream = typeof(SystemFunctionCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"в сборке нет словаря {ResourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
