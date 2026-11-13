using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>
/// Элемент таблицы системной функции FUNC_* (сверено с TIA на 148 функциях копий <c>data/real/steps-events/</c> 20–22,
/// V21): u32 1, u32 R, R ссылок, u32 подкод, u32 виды параметров — по 2 бита на параметр от старших (01 — ссылка, 10 —
/// VARIANT, 00 — конец), блоки VARIANT по 16 байт подряд, строки VARIANT — за ними. Ссылки на TIMER — не параметры
/// (StartProgram, ActivateCleanScreen, SimulateTag); остальные ссылки — параметры вида 01 по порядку.
/// </summary>
internal static class FunctionReader
{
    /// <summary>Первое слово элемента (у всех функций копий 1).</summary>
    private const int Marker = 1;

    /// <summary>Сколько параметров помещается в u32 видов (по 2 бита).</summary>
    private const int MaxParameters = 16;

    /// <summary>Ширина вида параметра в битах.</summary>
    private const int KindBits = 2;

    /// <summary>Маска вида параметра.</summary>
    private const long KindMask = 0b11;

    /// <summary>Вид «конец параметров».</summary>
    private const long EndKind = 0b00;

    /// <summary>Вид «ссылка» (тег, RECVAR).</summary>
    private const long ReferenceKind = 0b01;

    /// <summary>Вид «VARIANT».</summary>
    private const long VariantKind = 0b10;

    /// <summary>u32 подкод и u32 виды параметров за ссылками.</summary>
    private const int TailSize = 8;

    /// <summary>Смещение u32 видов параметров от конца ссылок: за u32 подкодом.</summary>
    private const int KindsOffset = 4;

    /// <summary>Таблица таймеров: ссылки на неё не параметры.</summary>
    private const string TimerTable = "TIMER";

    /// <summary>
    /// Прочитать функцию: подкод, параметры, имена из словаря (<see cref="SystemFunctionCatalog"/>; несколько имён и
    /// выбор по объекту — <see cref="Names"/>; первый параметр — ссылка, а у записи есть
    /// <see cref="SystemFunction.FirstReference"/> — имена и параметры из него). Незнакомый тип VARIANT — параметр «не известно» и предупреждение.
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица функции.</param>
    /// <param name="index">Номер элемента с нуля.</param>
    /// <returns>Вызов функции.</returns>
    /// <exception cref="FwxFormatException">
    /// Ссылка за последний элемент, элемент вне таблицы, первое слово не 1, ссылки, подкод, блоки или строки не
    /// помещаются, вид параметра 11, параметров-ссылок больше, чем ссылок, посредник тега или объект не сходится.
    /// </exception>
    public static FunctionCall Read(FwxDocument doc, TocEntry table, int index)
    {
        var item = ElementLink.Item(doc, table, index);
        var b = doc.Binary;
        var p = item.Offset;
        var (end, links) = ReferenceHeader.Read(b, item, table.Name);
        var marker = b.D4(p);
        if (marker != Marker)
        {
            throw new FwxFormatException(table.Name, p, $"запись {index + 1}: первое слово {marker}, ожидалось {Marker}");
        }
        if (end + TailSize > item.Length)
        {
            throw new FwxFormatException(table.Name, p, $"запись {index + 1}: подкод не помещается в {item.Length} байт");
        }
        var subcode = b.D4(p + end);
        var parameters = links.Where(l => doc.FindTable(l.Table)?.Name != TimerTable).ToList();
        var kinds = b.D4(p + end + KindsOffset);
        var arguments = Arguments(doc, table, item, end + TailSize, kinds, parameters);
        var entry = SystemFunctionCatalog.Find(table.Name, subcode);
        if (entry?.FirstReference is { } variant && ((kinds >> (32 - KindBits)) & KindMask) == ReferenceKind)
        {
            return new FunctionCall(new VarReference(table.Id, index), table.Name, subcode, variant.Names, variant.Parameters, arguments);
        }
        var call = new FunctionCall(
            new VarReference(table.Id, index),
            table.Name,
            subcode,
            Names(doc, entry, arguments),
            entry?.Parameters ?? [],
            arguments);
        return ScreenFunctionCalls.Project(doc, call);
    }

    /// <summary>Функция списка события; сбой — функция «не прочитана» и предупреждение, чтобы остальные функции события остались.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="function">Ссылка из списка; таблица есть в TOC (проверяет читатель списка).</param>
    /// <returns>Вызов функции; не прочитана — <see cref="FunctionCall.Arguments"/> <c>null</c>.</returns>
    public static FunctionCall ReadOrUnread(FwxDocument doc, VarReference function)
    {
        var table = doc.FindTable(function.Table)!;
        try
        {
            return Read(doc, table, function.Index);
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add(e.Message);
            return new FunctionCall(function, table.Name, null, [], [], null);
        }
    }

    /// <summary>Параметры по видам: ссылки — по порядку из <paramref name="links"/>, VARIANT — блоками подряд с <paramref name="start"/>.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица функции — для сообщений.</param>
    /// <param name="item">Элемент функции.</param>
    /// <param name="start">Смещение первого блока VARIANT от начала элемента.</param>
    /// <param name="kinds">u32 видов параметров.</param>
    /// <param name="links">Ссылки элемента без ссылок на TIMER.</param>
    /// <returns>Параметры по порядку.</returns>
    /// <exception cref="FwxFormatException">Вид 11, ссылок меньше, чем параметров-ссылок, блок не помещается в элемент.</exception>
    private static List<FunctionArgument> Arguments(FwxDocument doc, TocEntry table, TableItem item, long start, long kinds, List<VarReference> links)
    {
        var arguments = new List<FunctionArgument>();
        var nextLink = 0;
        var variant = start;
        for (var k = 0; k < MaxParameters; k++)
        {
            var kind = (kinds >> (32 - KindBits * (k + 1))) & KindMask;
            switch (kind)
            {
                case EndKind:
                    return arguments;
                case ReferenceKind when nextLink < links.Count:
                    arguments.Add(FunctionReferences.Argument(doc, links[nextLink++]));
                    break;
                case VariantKind when variant + FunctionValueReader.Size <= item.Length:
                    arguments.Add(FunctionValueReader.Read(doc, table, item, item.Offset + variant, k + 1));
                    variant += FunctionValueReader.Size;
                    break;
                default:
                    throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: параметр {k + 1} вида {kind} не читается (ссылок {links.Count}, длина {item.Length} байт)");
            }
        }
        return arguments;
    }

    /// <summary>
    /// Имена функции из словаря. Несколько имён и выбор по объекту (<see cref="SystemFunction.ObjectReferences"/>):
    /// элемент первого параметра-объекта ссылается на таблицу из выбора — одно имя, иначе все (RecipeView и
    /// SystemDiagnosticView на подкодах 0x1–0x4 — через « / », если у объекта нет ссылки на RECIPE_FACTORY).
    /// </summary>
    /// <returns>Имена; функции нет в словаре — пусто.</returns>
    /// <exception cref="FwxFormatException">Элемент объекта не сходится.</exception>
    private static IReadOnlyList<string> Names(FwxDocument doc, SystemFunction? entry, List<FunctionArgument> arguments)
    {
        if (entry?.ObjectReferences is not { Count: > 0 } choice)
        {
            return entry?.Names ?? [];
        }
        var screenObject = arguments.FirstOrDefault(a => a.Kind == FunctionArgumentKind.ScreenObject)?.Reference;
        if (screenObject is null)
        {
            return entry.Names;
        }
        var tables = ScreenObjects.ReferencedTables(doc, screenObject.Value);
        var chosen = choice.Where(c => tables.Contains(c.Key)).Select(c => c.Value).FirstOrDefault();
        return chosen is null ? entry.Names : [chosen];
    }
}
