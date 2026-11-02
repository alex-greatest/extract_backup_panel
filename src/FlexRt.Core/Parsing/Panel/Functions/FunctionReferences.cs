using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>
/// Параметр-ссылка функции (вид 01): тег — ссылка на ADAPTLOOPBREAKE, первая ссылка которого ведёт на запись VAR
/// (сверено с TIA на копиях <c>data/real/steps-events/</c>: внутренние теги, теги ПЛК с символьным доступом, с
/// абсолютным адресом и член DB), или прямо на VAR (ShiftAndMask, Encode, LookupText).
/// </summary>
internal static class FunctionReferences
{
    /// <summary>Таблица тегов.</summary>
    private const string VarTable = "VAR";

    /// <summary>Таблица-посредник тега: u32 0, u32 R = 1, ссылка на VAR (один элемент на тег, общий для функций).</summary>
    private const string LoopBreakTable = "ADAPTLOOPBREAKE";

    /// <summary>
    /// Таблица LIMIT: на её элемент с именем <c>тег~N</c> ссылается SetTag A603A0097, первая ссылка элемента — на
    /// ADAPTLOOPBREAKE тега (сверено с TIA через Openness: SetTag <c>Diag_SelectIn</c> 7 экранов A603A0097).
    /// </summary>
    private const string LimitTable = "LIMIT";

    /// <summary>Первое слово элемента LIMIT с именем (у Visibility экрана — u32 1).</summary>
    private const int NamedKind = 3;

    /// <summary>Сколько посредников проходится до VAR (LIMIT → ADAPTLOOPBREAKE → VAR — два).</summary>
    private const int MaxChain = 3;

    /// <summary>
    /// Значение параметра-ссылки: дошли до VAR через ADAPTLOOPBREAKE или LIMIT с именем — тег; ссылка на другую
    /// таблицу (RECVAR у LogTag) — <see cref="FunctionArgumentKind.Reference"/>; таблицы нет в TOC (Id 0 у
    /// GetDataRecordName) — <see cref="FunctionArgumentKind.Unknown"/>.
    /// </summary>
    /// <returns>Параметр.</returns>
    /// <exception cref="FwxFormatException">
    /// Посредник не сходится: ссылка за последний элемент, элемент вне таблицы, ссылки не помещаются в элемент.
    /// </exception>
    public static FunctionArgument Argument(FwxDocument doc, VarReference link)
    {
        for (var depth = 0; ; depth++)
        {
            var table = doc.FindTable(link.Table);
            if (table is null)
            {
                return new FunctionArgument(FunctionArgumentKind.Unknown);
            }
            if (table.Name == VarTable)
            {
                return new FunctionArgument(FunctionArgumentKind.Tag, Reference: link);
            }
            if ((depth < MaxChain ? Next(doc, table, link.Index) : null) is not { } next)
            {
                return new FunctionArgument(FunctionArgumentKind.Reference, Text: table.Name, Number: link.Index);
            }
            link = next;
        }
    }

    /// <summary>Первая ссылка посредника: элемента ADAPTLOOPBREAKE или элемента LIMIT с именем.</summary>
    /// <returns>Ссылка; таблица не посредник, у элемента нет ссылок или элемент LIMIT без имени — <c>null</c>.</returns>
    /// <exception cref="FwxFormatException">Элемент посредника не сходится.</exception>
    private static VarReference? Next(FwxDocument doc, TocEntry table, int index)
    {
        if (table.Name == LoopBreakTable)
        {
            var (_, links) = ReferenceHeader.Read(doc.Binary, ElementLink.Item(doc, table, index), table.Name);
            return links.Count > 0 ? links[0] : null;
        }
        if (table.Name != LimitTable)
        {
            return null;
        }
        var item = ElementLink.Item(doc, table, index);
        if (NamedElement.Kind(doc.Binary, item) != NamedKind)
        {
            return null;
        }
        var named = NamedElement.TryReferences(doc.Binary, item)
            ?? throw new FwxFormatException(table.Name, item.Offset, $"запись {index + 1}: ссылки не помещаются в {item.Length} байт");
        return named.Count > 0 ? named[0] : null;
    }
}
