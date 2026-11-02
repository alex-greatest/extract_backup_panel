using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>
/// Объект, на который ссылается параметр функции (VARIANT тип 3, слово +2 равно 2): объект экрана (VIEW_OCX,
/// QtContainer), окно Global screen (SCREEN_SYS) или соединение (CONNECTION_S7).
/// </summary>
internal static class ScreenObjects
{
    /// <summary>
    /// Параметр-объект: имя из элемента объекта (<see cref="NamedElement"/>: <c>TV1</c>, <c>HB1</c>, <c>Conn_S7</c>;
    /// сверено с TIA на всех 9 объектах функций копий 21–22); имя пустое или не помещается — имя таблицы и номер
    /// элемента с единицы (<c>SCREEN_SYS 7</c>: у окон Global screen имени в файле нет).
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="tableId">Id таблицы объекта.</param>
    /// <param name="index">Номер элемента с нуля.</param>
    /// <returns>Параметр <see cref="FunctionArgumentKind.ScreenObject"/>; таблицы нет в TOC — <see cref="FunctionArgumentKind.Unknown"/>.</returns>
    /// <exception cref="FwxFormatException">Ссылка ведёт за последний элемент таблицы или элемент выходит за таблицу.</exception>
    public static FunctionArgument Argument(FwxDocument doc, int tableId, int index)
    {
        var table = doc.FindTable(tableId);
        if (table is null)
        {
            return new FunctionArgument(FunctionArgumentKind.Unknown);
        }
        var name = NamedElement.TryName(doc.Binary, ElementLink.Item(doc, table, index));
        var text = string.IsNullOrEmpty(name) ? $"{table.Name} {index + 1}" : name;
        return new FunctionArgument(FunctionArgumentKind.ScreenObject, Text: text, Reference: new VarReference(tableId, index));
    }

    /// <summary>
    /// Имена таблиц, на которые ссылается элемент объекта (у RecipeView есть ссылка на RECIPE_FACTORY — наблюдение на
    /// 3 объектах копии 22).
    /// </summary>
    /// <returns>Имена таблиц из TOC; таблицы объекта нет в TOC или ссылки не читаются — пусто.</returns>
    /// <exception cref="FwxFormatException">Ссылка ведёт за последний элемент таблицы или элемент выходит за таблицу.</exception>
    public static IReadOnlySet<string> ReferencedTables(FwxDocument doc, VarReference screenObject)
    {
        var table = doc.FindTable(screenObject.Table);
        if (table is null)
        {
            return new HashSet<string>();
        }
        var links = NamedElement.TryReferences(doc.Binary, ElementLink.Item(doc, table, screenObject.Index)) ?? [];
        return links.Select(l => doc.FindTable(l.Table)?.Name).OfType<string>().ToHashSet();
    }
}
