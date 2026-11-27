using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel.Functions;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Список функций события экрана (сверено с TIA на копиях <c>data/real/steps-events/</c> 09–24, V21): FUNC_LIST —
/// u32 1, u32 R, R ссылок (у Loaded — ADAPTDEMULTIPLE условия «текущий экран = номер экрана», не читается), u16 N,
/// N пар (u16 Id таблицы функции, u16 номер элемента); ADAPTDEMULTIPLE события Cleared — u32 0, u32 R = 1, ссылка на
/// FUNC_LIST или на одну функцию FUNC_* (во всех 26 ссылках экранов 84 копий; элементы со ссылкой на VARSYS —
/// условия Loaded, экраны на них не ссылаются), u32 (0).
/// </summary>
internal static class FunctionListReader
{
    /// <summary>Первое слово элемента FUNC_LIST.</summary>
    private const int ListMarker = 1;

    /// <summary>Число ссылок элемента ADAPTDEMULTIPLE события Cleared.</summary>
    private const int DemultiplexerReferences = 1;

    /// <summary>Размер u32 значения за ссылкой элемента ADAPTDEMULTIPLE.</summary>
    private const int ValueSize = 4;

    /// <summary>Размер u16 числа функций списка.</summary>
    private const int CountSize = 2;

    /// <summary>Размер пары (u16 Id таблицы, u16 номер элемента).</summary>
    private const int PairSize = 4;

    /// <summary>Таблица списка функций: на неё ссылается экран с событием Loaded и ADAPTDEMULTIPLE события Cleared с двумя функциями и больше.</summary>
    internal const string ListTable = "FUNC_LIST";

    /// <summary>Начало имени таблиц функций (FUNC_BIT, FUNC_ASSIGN, …) и списка FUNC_LIST.</summary>
    private const string FunctionPrefix = "FUNC_";

    /// <summary>Таблица функций или списка, включая таблицы словаря без префикса FUNC_.</summary>
    /// <returns>Распознано ли имя таблицы функций.</returns>
    internal static bool IsFunctionTable(string name) =>
        name.StartsWith(FunctionPrefix, StringComparison.Ordinal) || SystemFunctionCatalog.HasTable(name);

    /// <summary>Функции Loaded: элемент FUNC_LIST, на который ссылается экран.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица FUNC_LIST.</param>
    /// <param name="screen">Экран — для сообщения о ссылке за последний элемент.</param>
    /// <param name="index">Номер элемента из ссылки экрана, с нуля.</param>
    /// <returns>Ссылки на функции в порядке TIA; таблица каждой есть в TOC.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последний элемент или элемент списка не сходится.</exception>
    public static IReadOnlyList<VarReference> Loaded(FwxDocument doc, TocEntry table, Screen screen, int index) =>
        List(doc, table, ElementLink.Item(doc, table, index, Owner(screen)));

    /// <summary>Функции Cleared: элемент ADAPTDEMULTIPLE, на который ссылается экран, ведёт на FUNC_LIST или на одну функцию.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица ADAPTDEMULTIPLE.</param>
    /// <param name="screen">Экран — для сообщения о ссылке за последний элемент.</param>
    /// <param name="index">Номер элемента из ссылки экрана, с нуля.</param>
    /// <returns>Ссылки на функции в порядке TIA; таблица каждой есть в TOC.</returns>
    /// <exception cref="FwxFormatException">
    /// Ссылка за последний элемент, элемент ADAPTDEMULTIPLE или списка не сходится, R не 1, таблицы ссылки нет в TOC,
    /// ссылка ведёт не на FUNC_LIST и не на функцию FUNC_* (решение пользователя: сломан ADAPTDEMULTIPLE — событие
    /// «не прочитано»).
    /// </exception>
    public static IReadOnlyList<VarReference> Cleared(FwxDocument doc, TocEntry table, Screen screen, int index)
    {
        var (item, _, target) = Demultiplexer(doc, table, index, Owner(screen));
        return Functions(doc, table, item, target);
    }

    /// <summary>
    /// Элемент ADAPTDEMULTIPLE: функции по его единственной ссылке и u32 за ссылками — значение (у Cleared экрана 0, у
    /// события объекта 1 — Activate, 0 — Deactivate; шаги <c>data/real/steps-iofield/</c> 82, 83, 85).
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица ADAPTDEMULTIPLE.</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения о ссылке за последний элемент (<c>экран Screen_2</c>).</param>
    /// <returns>Ссылки на функции в порядке TIA и значение.</returns>
    /// <exception cref="FwxFormatException">
    /// Ссылка за последний элемент, элемент или список не сходится, R не 1, значение не помещается, ссылка ведёт не на
    /// FUNC_LIST и не на функцию FUNC_*.
    /// </exception>
    public static (IReadOnlyList<VarReference> Functions, long Value) Demultiplexed(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var (item, target, value) = DemultiplexedTarget(doc, table, index, owner);
        return (Functions(doc, table, item, target), value);
    }

    /// <summary>Прочитать условие ADAPTDEMULTIPLE и ссылку на список без чтения целевой записи.</summary>
    /// <returns>Элемент, ссылка на действие и значение условия.</returns>
    /// <exception cref="FwxFormatException">Элемент посредника или его значение повреждены.</exception>
    public static (TableItem Item, VarReference Target, long Value) DemultiplexedTarget(
        FwxDocument doc, TocEntry table, int index, string owner)
    {
        var (item, end, target) = Demultiplexer(doc, table, index, owner);
        return end + ValueSize <= item.Length
            ? (item, target, doc.Binary.D4(item.Offset + end))
            : throw new FwxFormatException(table.Name, item.Offset, $"запись {index + 1}: значение не помещается в {item.Length} байт");
    }

    /// <summary>Элемент ADAPTDEMULTIPLE с единственной ссылкой.</summary>
    /// <returns>Элемент, конец ссылок от его начала и ссылка.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последний элемент, элемент не сходится, R не 1.</exception>
    private static (TableItem Item, long End, VarReference Target) Demultiplexer(FwxDocument doc, TocEntry table, int index, string owner)
    {
        var item = ElementLink.Item(doc, table, index, owner);
        var (end, links) = ReferenceHeader.Read(doc.Binary, item, table.Name);
        return links.Count == DemultiplexerReferences
            ? (item, end, links[0])
            : throw new FwxFormatException(table.Name, item.Offset, $"запись {index + 1}: {links.Count} ссылок, ожидалась {DemultiplexerReferences}");
    }

    /// <summary>
    /// Функции по ссылке элемента-посредника: на FUNC_LIST — его список, на функцию (таблица FUNC_* или таблица словаря
    /// функций — WEB_FUNC, COPYARCHIVES) — она одна.
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица элемента со ссылкой — для сообщений.</param>
    /// <param name="item">Элемент со ссылкой — для сообщений.</param>
    /// <param name="target">Ссылка на функцию или список.</param>
    /// <returns>Ссылки на функции в порядке TIA; таблица каждой есть в TOC.</returns>
    /// <exception cref="FwxFormatException">
    /// Таблицы ссылки нет в TOC, ссылка ведёт не на FUNC_LIST, не на FUNC_* и не на таблицу словаря, список не сходится.
    /// </exception>
    public static IReadOnlyList<VarReference> Functions(FwxDocument doc, TocEntry table, TableItem item, VarReference target)
    {
        var targetTable = TableOf(doc, table, item, target);
        if (!IsFunctionTable(targetTable.Name))
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: ссылка на {targetTable.Name}, ожидалась FUNC_LIST или функция FUNC_*");
        }
        return targetTable.Name == ListTable
            ? List(doc, targetTable, ElementLink.Item(doc, targetTable, target.Index))
            : [target];
    }

    /// <summary>Функции объекта по ссылке, при праве через SECURITY_MONITO и его ссылку на действие.</summary>
    /// <returns>Ссылки на функции в исходном порядке.</returns>
    /// <exception cref="FwxFormatException">Ссылка, обёртка права или список повреждены.</exception>
    internal static IReadOnlyList<VarReference> AuthorizedFunctions(
        FwxDocument doc, TocEntry owner, TableItem item, VarReference link, string who, string missingActionContext = "")
    {
        if (doc.FindTable(link.Table) is not { Name: "SECURITY_MONITO" } table)
        {
            return Functions(doc, owner, item, link);
        }
        var security = ElementLink.Item(doc, table, link.Index, who);
        var (_, links) = ReferenceHeader.Read(doc.Binary, security, table.Name);
        foreach (var action in links)
        {
            if (doc.FindTable(action.Table)?.Name != "SECURITY_UNIT")
            {
                return Functions(doc, table, security, action);
            }
        }
        throw new FwxFormatException(table.Name, security.Offset, $"запись {link.Index + 1}: нет ссылки на функцию{missingActionContext}");
    }

    /// <summary>Чья ссылка — для сообщения о ссылке за последний элемент.</summary>
    /// <returns>Текст <c>экран Screen_2</c>.</returns>
    private static string Owner(Screen screen) => $"экран {screen.Name}";

    /// <summary>Пары функций элемента FUNC_LIST.</summary>
    /// <returns>Ссылки на функции по порядку.</returns>
    /// <exception cref="FwxFormatException">Первое слово не 1, ссылки или пары не помещаются, таблицы функции нет в TOC.</exception>
    private static List<VarReference> List(FwxDocument doc, TocEntry table, TableItem item)
    {
        var b = doc.Binary;
        var p = item.Offset;
        var (end, _) = ReferenceHeader.Read(b, item, table.Name);
        var marker = b.D4(p);
        if (marker != ListMarker)
        {
            throw new FwxFormatException(table.Name, p, $"запись {item.Index + 1}: первое слово {marker}, ожидалось {ListMarker}");
        }
        var count = end + CountSize <= item.Length ? b.D2(p + end) : -1;
        if (count < 0 || end + CountSize + count * PairSize > item.Length)
        {
            throw new FwxFormatException(table.Name, p, $"запись {item.Index + 1}: функции ({count}) не помещаются в {item.Length} байт");
        }
        var pairs = p + end + CountSize;
        var functions = new List<VarReference>();
        for (var k = 0; k < count; k++)
        {
            var function = ReferenceHeader.ReadPair(b, pairs + k * PairSize);
            TableOf(doc, table, item, function);
            functions.Add(function);
        }
        return functions;
    }

    /// <summary>Таблица ссылки списка или ADAPTDEMULTIPLE.</summary>
    /// <returns>Таблица TOC.</returns>
    /// <exception cref="FwxFormatException">Id таблицы нет в TOC.</exception>
    private static TocEntry TableOf(FwxDocument doc, TocEntry table, TableItem item, VarReference link) =>
        doc.FindTable(link.Table)
        ?? throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: таблицы 0x{link.Table:x} нет в TOC");
}
