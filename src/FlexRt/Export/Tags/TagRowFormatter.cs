using System.Text.RegularExpressions;
using FlexRt.Model.Matching;
using FlexRt.Model.Panel;
using FlexRt.Parsing.Codes;

namespace FlexRt.Export.Tags;

/// <summary>Строка листа «Теги»: значения 11 колонок для одного тега панели.</summary>
public static partial class TagRowFormatter
{
    /// <summary>Одномерный массив ПЛК: <c>Array[1..1000] of Real</c>.</summary>
    [GeneratedRegex(@"^Array\s*\[(-?\d+)\.\.(-?\d+)\] of (.+)$")]
    private static partial Regex ArrayType();

    /// <summary>Текст ячейки, если данных нет: нет файла ПЛК, он сломан или ПЛК не выбран.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>Текст ячейки, если ПЛК выбран, но тега в нём нет.</summary>
    private const string NotInPlcFile = "отсутствует в файле ПЛК";

    /// <summary>
    /// Значения колонок Name, Data type, Connection, PLC name, PLC tag, Address, Access mode,
    /// Acquisition cycle, Logged, Source comment, Comment. Внутренний тег: Connection
    /// <c>&lt;Internal tag&gt;</c>, колонки ПЛК пустые. PLC-тег: соединение, режим доступа и
    /// цикл — из панели; адрес — только при абсолютном доступе; тип, PLC name, PLC tag и
    /// Source comment — из проекта ПЛК, иначе «неизвестно» или «отсутствует в файле ПЛК».
    /// Logged и Comment пустые: в файлах их нет.
    /// </summary>
    /// <returns>11 значений.</returns>
    public static string[] Format(FwxDocument doc, HmiTag tag, PlcMatch? match)
    {
        if (tag.LinkTable is null)
        {
            return [tag.Name, TagTypeNames.Format(tag), "<Internal tag>", "", "", "", "", "", "", "", ""];
        }
        if (match?.Link is not { } link)
        {
            return [tag.Name, Unknown, Unknown, Unknown, Unknown, Unknown, Unknown, Unknown, "", Unknown, ""];
        }

        var found = match.Lookup == PlcLookup.Found ? match.Tag : null;
        return
        [
            tag.Name,
            found?.DataType is { } plcType ? HmiStyle(plcType) : TypeName(link),
            link.Connection < doc.Connections.Count ? doc.Connections[link.Connection].Name : Unknown,
            match.Lookup == PlcLookup.AddressOnly ? match.PlcName ?? Unknown : FromPlc(match, match.PlcName),
            FromPlc(match, found?.Name),
            link.Absolute ? PlcAddress.Format(link) ?? Unknown : "",
            link.Absolute ? "<absolute access>" : "<symbolic access>",
            Cycle(link.CycleMs),
            "",
            FromPlc(match, found?.Comment),
            ""
        ];
    }

    /// <summary>
    /// Значение из проекта ПЛК: найдено — оно (или «неизвестно», если пусто у имени ПЛК), не
    /// найдено — пометка, абсолютный тег без символа — пусто, как в TIA.
    /// </summary>
    /// <returns>Текст ячейки.</returns>
    private static string FromPlc(PlcMatch? match, string? value) => match?.Lookup switch
    {
        PlcLookup.Found => value ?? Unknown,
        PlcLookup.NotInPlcFile => NotInPlcFile,
        PlcLookup.AddressOnly => "",
        _ => Unknown
    };

    /// <summary>
    /// Тип по коду ПЛК из панели: один тип — его имя, неразличимая пара — через косую черту
    /// (<c>USInt/Char</c>), неизвестный код — <c>?</c>. Строка — <c>String</c> без длины, массив
    /// — <c>Array [0..N-1] of Real</c>, как в колонке Data type HMI-тега в TIA (сверено с
    /// Openness-экспортом проекта A603A0097).
    /// </summary>
    /// <returns>Название типа.</returns>
    private static string TypeName(PlcLink link)
    {
        var names = PlcTypeCodes.Candidates(link);
        var name = names.Count == 0 ? "?" : string.Join("/", names);
        return link.Elements > 1 && name != "String" ? $"Array [0..{link.Elements - 1}] of {name}" : name;
    }

    /// <summary>
    /// Тип ПЛК в виде колонки Data type HMI-тега в TIA: <c>String[30]</c> → <c>String</c>,
    /// одномерный <c>Array[1..1000] of Real</c> → <c>Array [0..999] of Real</c> (у HMI-тега
    /// нижняя граница всегда 0). Остальное как есть.
    /// </summary>
    /// <returns>Название типа.</returns>
    private static string HmiStyle(string plcType)
    {
        if (plcType.StartsWith("String[", StringComparison.Ordinal))
        {
            return "String";
        }
        var match = ArrayType().Match(plcType);
        if (!match.Success || !long.TryParse(match.Groups[1].Value, out var low) || !long.TryParse(match.Groups[2].Value, out var high))
        {
            return plcType;
        }
        return $"Array [0..{high - low}] of {HmiStyle(match.Groups[3].Value)}";
    }

    /// <summary>
    /// Цикл опроса как в TIA: кратно часу — <c>1 h</c>, минуте — <c>1 min</c>, секунде —
    /// <c>1 s</c>, иначе <c>100 ms</c>. С проектом сверены 100 мс, 500 мс, 1 с и 10 с; имя
    /// пользовательского цикла в файле не хранится. 0 — цикла в элементе нет (наблюдение:
    /// у указателей областей в таблице DATALINK), ячейка пустая.
    /// </summary>
    /// <returns>Текст цикла.</returns>
    private static string Cycle(long ms) => ms switch
    {
        0 => "",
        > 0 when ms % 3_600_000 == 0 => $"{ms / 3_600_000} h",
        > 0 when ms % 60_000 == 0 => $"{ms / 60_000} min",
        > 0 when ms % 1000 == 0 => $"{ms / 1000} s",
        _ => $"{ms} ms"
    };
}
