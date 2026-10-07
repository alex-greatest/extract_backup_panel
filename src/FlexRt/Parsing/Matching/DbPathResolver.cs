using System.Text.RegularExpressions;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Codes;

namespace FlexRt.Parsing.Matching;

/// <summary>
/// Разрешение символьного пути pdata (<see cref="PlcLink.Path"/>) в член DB проекта ПЛК.
/// Элемент пути: старший полубайт — вид, остальное — значение. Вид 2 — промежуточный член,
/// 4 — конечный (значение — LID члена внутри родителя), 5 — индекс элемента массива (от
/// нижней границы), 6 и 7 — хвост строки (пропускаются).
/// </summary>
public static partial class DbPathResolver
{
    /// <summary>Вид элемента пути: промежуточный член.</summary>
    private const int KindMember = 2;

    /// <summary>Вид элемента пути: конечный член.</summary>
    private const int KindLastMember = 4;

    /// <summary>Вид элемента пути: индекс массива.</summary>
    private const int KindIndex = 5;

    /// <summary>Сдвиг вида в элементе пути.</summary>
    private const int KindShift = 28;

    /// <summary>Маска значения в элементе пути.</summary>
    private const long ValueMask = 0x0fffffff;

    /// <summary>Тип-массив: <c>Array[1..10,0..3] of Real</c>.</summary>
    [GeneratedRegex(@"^Array\[([^\]]*)\] of (.*)$")]
    private static partial Regex ArrayType();

    /// <summary>
    /// Найти член DB по номеру и пути. Подходят DB выбранного ПЛК с этим номером; для каждого
    /// пробуются его корни интерфейса по порядку, берётся первый, где путь сошёлся. Нет такого
    /// DB или путь не сошёлся — <see cref="PlcLookup.NotInPlcFile"/>; не сошёлся, а у DB
    /// с этим номером были проблемы чтения — <see cref="PlcLookup.Unknown"/>; DB с таким номером
    /// нет, а в проекте есть DB без номера — тоже <see cref="PlcLookup.Unknown"/>.
    /// </summary>
    /// <returns>Результат разрешения.</returns>
    public static DbResolution Resolve(PlcProject project, long plcId, PlcLink link)
    {
        var dbs = project.Dbs.Where(d => d.Number == link.DbNumber && d.PlcId == plcId).ToList();
        var problem = dbs.Count == 0 && project.UnnumberedDbs > 0;
        foreach (var db in dbs)
        {
            if (db.Interfaces.Select(root => Walk(db, root, link.Path)).FirstOrDefault(r => r is not null) is { } found)
            {
                return found;
            }
            problem |= db.Problem is not null;
        }
        return new DbResolution(problem ? PlcLookup.Unknown : PlcLookup.NotInPlcFile, "", "", "");
    }

    /// <summary>
    /// Пройти путь от корня. На каждом уровне ищется член с нужным LID; тип <c>"UDT"</c> ведёт
    /// в корень UDT из <see cref="PlcDbInterface.Udts"/> корня DB; индекс дописывается к
    /// последнему имени как <c>[i]</c> (несколько измерений — <c>[i,j]</c>). Комментарий —
    /// у последнего члена пути из <see cref="PlcDbInterface.Comments"/> того корня, где член
    /// найден, по ключу «цепочка ID от этого корня»: у члена в DB — корень DB, у члена внутри
    /// UDT — корень UDT; комментарий элемента массива — комментарий самого массива.
    /// </summary>
    /// <returns>Путь, тип и комментарий или <c>null</c>, если член не найден.</returns>
    private static DbResolution? Walk(PlcDb db, PlcDbInterface top, IReadOnlyList<long> elements)
    {
        var current = top;
        var prefix = "";
        var segments = new List<(string Name, string Index)>();
        PlcDbMember? leaf = null;
        var comment = "";
        var dimensions = new List<string>();
        var indexed = 0;
        foreach (var element in elements)
        {
            var kind = (int)(element >> KindShift);
            var value = element & ValueMask;
            switch (kind)
            {
                case KindMember or KindLastMember:
                {
                    leaf = Candidates(current, prefix).FirstOrDefault(m => m.Lid == value);
                    if (leaf is null)
                    {
                        return null;
                    }
                    segments.Add((leaf.Name, ""));
                    comment = current.Comments.GetValueOrDefault(ChildKey(prefix, leaf), "");
                    indexed = 0;
                    var (leafDimensions, elementType) = SplitArray(PlcMemberTypes.Of(leaf));
                    dimensions = leafDimensions;
                    var next = Descend(top, current, prefix, leaf, elementType);
                    if (next is null)
                    {
                        return null;
                    }
                    (current, prefix) = next.Value;
                    break;
                }
                case KindIndex:
                {
                    if (segments.Count == 0)
                    {
                        return null;
                    }
                    var index = LowerBound(dimensions, indexed) + value;
                    segments[^1] = (segments[^1].Name, AppendIndex(segments[^1].Index, index));
                    indexed++;
                    break;
                }
            }
        }
        return leaf is null ? null : new DbResolution(PlcLookup.Found, FormatPath(db, segments), ElementType(PlcMemberTypes.Of(leaf), indexed), comment);
    }

    /// <summary>Члены на уровне: у корня — верхние (или безымянные разделы FB), глубже — по <c>ParentId</c>.</summary>
    /// <returns>Члены уровня.</returns>
    private static IReadOnlyList<PlcDbMember> Candidates(PlcDbInterface root, string prefix)
    {
        if (prefix != "")
        {
            return root.Kids.TryGetValue(prefix, out var kids) ? kids : [];
        }
        var sectionsOnly = root.Top.Count == 0 || root.Top[0].Lid < 0;
        return sectionsOnly && root.Kids.TryGetValue("", out var sections) ? sections : root.Top;
    }

    /// <summary>Ключ члена в словарях <see cref="PlcDbInterface.Kids"/> и <see cref="PlcDbInterface.Comments"/>: цепочка ID через <c>:</c>.</summary>
    /// <returns>Цепочка ID от корня до члена включительно.</returns>
    private static string ChildKey(string prefix, PlcDbMember member) => prefix == "" ? member.Id : prefix + ":" + member.Id;

    /// <summary>
    /// Куда идти дальше от члена: тип в кавычках или имя из словаря UDT — в корень UDT с пустым
    /// префиксом; иначе — в детей члена в том же корне.
    /// </summary>
    /// <returns>Следующий корень и префикс или <c>null</c>, если UDT не связан.</returns>
    private static (PlcDbInterface Root, string Prefix)? Descend(
        PlcDbInterface top, PlcDbInterface current, string prefix, PlcDbMember member, string elementType)
    {
        var baseName = elementType.Trim('"');
        if (!elementType.StartsWith('"') && !top.Udts.ContainsKey(baseName))
        {
            return (current, ChildKey(prefix, member));
        }
        return top.Udts.TryGetValue(baseName, out var udt) ? (udt, "") : null;
    }

    /// <summary>Разбить тип-массив на границы измерений и тип элемента.</summary>
    /// <returns>Строки границ <c>l..u</c> (пусто, если не массив) и тип элемента.</returns>
    private static (List<string> Dimensions, string Element) SplitArray(string type)
    {
        var match = ArrayType().Match(type);
        return match.Success ? (match.Groups[1].Value.Split(',').Select(d => d.Trim()).ToList(), match.Groups[2].Value) : ([], type);
    }

    /// <summary>Нижняя граница измерения; нет измерения или граница не число (<c>*</c>) — 0.</summary>
    /// <returns>Нижняя граница.</returns>
    private static long LowerBound(List<string> dimensions, int dimension)
    {
        if (dimension >= dimensions.Count)
        {
            return 0;
        }
        return long.TryParse(dimensions[dimension].Split("..")[0], out var low) ? low : 0;
    }

    /// <summary>Добавить индекс к строке индексов: <c>""</c> → <c>[3]</c> → <c>[3,4]</c>.</summary>
    /// <returns>Новая строка индексов.</returns>
    private static string AppendIndex(string indexes, long index) => indexes == "" ? $"[{index}]" : $"{indexes[..^1]},{index}]";

    /// <summary>
    /// Путь в виде, в каком TIA хранит его в объекте 0x25021: имя DB, затем члены через точку;
    /// имя с символом, кроме буквы, цифры и <c>_</c>, — в кавычках (<c>"Scan Requested"</c>);
    /// имя, начинающееся с цифры, — без кавычек (<c>2GraphIn1Chart</c>).
    /// </summary>
    /// <returns>Текст пути.</returns>
    private static string FormatPath(PlcDb db, List<(string Name, string Index)> segments)
    {
        var parts = segments.Select(s => Quote(s.Name) + s.Index);
        return string.Join('.', parts.Prepend(Quote(db.Name ?? db.Number.ToString())));
    }

    /// <summary>Имя в кавычках, если в нём есть символ, кроме буквы, цифры и <c>_</c>.</summary>
    /// <returns>Имя для пути.</returns>
    private static string Quote(string name) => name.All(c => char.IsLetterOrDigit(c) || c == '_') ? name : $"\"{name}\"";

    /// <summary>
    /// Тип члена с учётом индексов: все измерения проиндексированы — тип элемента, часть —
    /// <c>Array[оставшиеся] of тип</c>, индексов нет — тип члена как есть.
    /// </summary>
    /// <returns>Тип для колонки Data type.</returns>
    private static string ElementType(string memberType, int indexed)
    {
        var (dimensions, element) = SplitArray(memberType);
        if (indexed == 0 || dimensions.Count == 0)
        {
            return memberType;
        }
        return indexed >= dimensions.Count ? element : $"Array[{string.Join(",", dimensions.Skip(indexed))}] of {element}";
    }
}
