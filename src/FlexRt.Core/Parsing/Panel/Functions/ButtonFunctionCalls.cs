using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>Пары таблица/подкод и варианты параметров, подтверждённые компиляциями событий Button.</summary>
internal static class ButtonFunctionCalls
{
    /// <summary>Дополнить общий разбор функциями и вариантами из списка Button.</summary>
    /// <returns>Вызов с именами и параметрами в порядке TIA.</returns>
    public static FunctionCall Project(FwxDocument doc, FunctionCall call)
    {
        if (call.Arguments is not { } arguments)
        {
            return call;
        }
        return (call.Table, call.Subcode) switch
        {
            ("FUNC_NOTIFYUSER", 0) => Named(call, "NotifyUserAction",
                ["Confirmation type", "Mandatory commenting", "Category", "Object name", "Description"]),
            ("FUNC_ARCHVAR", 0) => LogTag(doc, call, arguments),
            ("FUNC_SETGETPLCM", 0) => Named(call, "SetPLCMode", ["Connection", "Mode"]),
            ("FUNC_SETGETPLCM", 1) => GetPlcMode(doc, call, arguments),
            ("FUNC_DATETIME_S", 3) => Named(call, "SetPLCDateTime", ["Connection", "Time"]),
            ("FUNC_SETDAYLIGH", 0) => Named(call, "SetDaylightSavingTime", ["Daylight saving time"]),
            ("FUNC_CHANGE_PLC", 0) when arguments is [_, { Kind: FunctionArgumentKind.Text }, { Kind: FunctionArgumentKind.Text }]
                => Named(call, "ChangeConnectionEIP", ["Connection", "Address", "Communication path"]),
            ("FUNC_COMMAND", 0x9) => Named(call, "PLCCodeViewActualOrTriggerValues", ["Screen object"]),
            ("FUNC_COMMAND", 0xa) => Named(call, "PLCCodeViewSupervisionOrFBCall", ["Screen object"]),
            ("FUNC_COMMAND", 0x404) => UpdateView(doc, call, arguments),
            _ => call
        };
    }

    /// <summary>LogTag кнопки: RECVAR с одной ссылкой на VAR содержит тег. Первый аргумент вызова — внутренний номер журнала.</summary>
    /// <returns>Вызов с тегом из VAR или исходные аргументы другой раскладки.</returns>
    private static FunctionCall LogTag(FwxDocument doc, FunctionCall call, IReadOnlyList<FunctionArgument> arguments)
    {
        if (arguments is not [{ Kind: FunctionArgumentKind.ProjectObject, ObjectKind: 0 },
                { Kind: FunctionArgumentKind.Reference, Text: "RECVAR", Number: var index }]
            || doc.FindTable("RECVAR") is not { } table)
        {
            return call;
        }
        var item = ElementLink.Item(doc, table, (int)index);
        var links = ReferenceHeader.TryReadAt(doc.Binary, item, 4);
        return links is [var tag] && doc.Binary.D4(item.Offset) == 1 && doc.FindTable(tag.Table) is { Name: "VAR" } tags
            && tag.Index < tags.Entries
            ? call with { ParameterNames = ["Tag"], Arguments = [new FunctionArgument(FunctionArgumentKind.Tag, Reference: tag)] }
            : call;
    }

    /// <summary>GetPLCMode: выходной тег дополнительно указан в ссылках функции после DATALINK.</summary>
    /// <returns>Вызов с именем выходного параметра и тегом, если ссылка на него прочитана.</returns>
    private static FunctionCall GetPlcMode(FwxDocument doc, FunctionCall call, IReadOnlyList<FunctionArgument> arguments)
    {
        var named = Named(call, "GetPLCMode", ["Connection", "Mode (Output)"]);
        if (arguments is not [_, { Kind: FunctionArgumentKind.Reference, Text: "DATALINK" }]
            || doc.FindTable(call.Element.Table) is not { } table)
        {
            return named;
        }
        var item = ElementLink.Item(doc, table, call.Element.Index);
        var (_, links) = ReferenceHeader.Read(doc.Binary, item, table.Name);
        var tag = links.Where(link => doc.FindTable(link.Table)?.Name == "ADAPTLOOPBREAKE")
            .Select(link => FunctionReferences.Argument(doc, link))
            .FirstOrDefault(argument => argument.Kind == FunctionArgumentKind.Tag);
        return tag is null ? named : named with { Arguments = [arguments[0], tag] };
    }

    /// <summary>FUNC_COMMAND 0x404 общий для AlarmView и SystemDiagnosticView.</summary>
    /// <returns>AlarmView при его подтверждённой ссылке на MSG_ACKFOCUS, иначе оба имени.</returns>
    private static FunctionCall UpdateView(FwxDocument doc, FunctionCall call, IReadOnlyList<FunctionArgument> arguments)
    {
        const string alarm = "AlarmViewUpdate";
        const string diagnostic = "SystemDiagnosticViewUpdatePlcBuffer";
        if (arguments is [{ Reference: { } target }] && ScreenObjects.ReferencedTables(doc, target).Contains("MSG_ACKFOCUS"))
        {
            return Named(call, alarm, ["Screen object"]);
        }
        return call with { Names = [alarm, diagnostic], ParameterNames = ["Screen object"] };
    }

    /// <summary>Установить имя функции и имена её параметров.</summary>
    /// <returns>Вызов с теми же прочитанными значениями.</returns>
    private static FunctionCall Named(FunctionCall call, string name, IReadOnlyList<string> parameters) =>
        call with { Names = [name], ParameterNames = parameters };
}
