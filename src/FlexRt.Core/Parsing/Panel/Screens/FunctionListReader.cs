using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

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

    /// <summary>Размер u16 числа функций списка.</summary>
    private const int CountSize = 2;

    /// <summary>Размер пары (u16 Id таблицы, u16 номер элемента).</summary>
    private const int PairSize = 4;

    /// <summary>Таблица списка функций: на неё ссылается экран с событием Loaded и ADAPTDEMULTIPLE события Cleared с двумя функциями и больше.</summary>
    internal const string ListTable = "FUNC_LIST";

    /// <summary>Начало имени таблиц функций (FUNC_BIT, FUNC_ASSIGN, …) и списка FUNC_LIST.</summary>
    private const string FunctionPrefix = "FUNC_";

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
        var item = ElementLink.Item(doc, table, index, Owner(screen));
        var (_, links) = ReferenceHeader.Read(doc.Binary, item, table.Name);
        if (links.Count != DemultiplexerReferences)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {index + 1}: {links.Count} ссылок, ожидалась {DemultiplexerReferences}");
        }
        var target = links[0];
        var targetTable = TableOf(doc, table, item, target);
        if (!targetTable.Name.StartsWith(FunctionPrefix, StringComparison.Ordinal))
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {index + 1}: ссылка на {targetTable.Name}, ожидалась FUNC_LIST или функция FUNC_*");
        }
        return targetTable.Name == ListTable
            ? List(doc, targetTable, ElementLink.Item(doc, targetTable, target.Index))
            : [target];
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
            var function = new VarReference(b.D2(pairs + k * PairSize), b.D2(pairs + k * PairSize + 2));
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
