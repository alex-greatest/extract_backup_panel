using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel.Functions;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>События VIEW_BUTTONS и VIEW_STATEBUTTO, подтверждённые на шагах Project1, V21, TP700 Comfort.</summary>
internal static class ButtonEventReader
{
    /// <summary>Второе слово ссылки кнопки на Press.</summary>
    private const int PressWord = 0x8019;

    /// <summary>Второе слово ссылки кнопки на Release.</summary>
    private const int ReleaseWord = 0x801a;

    /// <summary>Click у VIEW_BUTTONS и Change у VIEW_STATEBUTTO.</summary>
    private const int ClickWord = 0x8000;

    /// <summary>Change у VIEW_BUTTONS и Click у VIEW_STATEBUTTO.</summary>
    private const int ChangeWord = 0x801b;

    /// <summary>Ссылка на ADAPTDEMULTIPLE у Activate и Deactivate.</summary>
    private const int FocusWord = 0x8039;

    /// <summary>Размер одной ссылки.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Смещение второго служебного слова от начала ссылки.</summary>
    private const int WordOffset = 6;

    /// <summary>Прочитать назначения кнопки. Ошибка цепочки сохраняет кнопку и остальные события.</summary>
    /// <returns>Считываемые события в порядке ссылок.</returns>
    public static IReadOnlyList<ObjectEvent> Read(
        FwxDocument doc, TocEntry table, TableItem item, long end, IReadOnlyList<VarReference> links)
    {
        var events = new Dictionary<ObjectEventKind, ObjectEvent>();
        var state = table.Name == "VIEW_STATEBUTTO";
        for (var index = 0; index < links.Count; index++)
        {
            var word = doc.Binary.D2(item.Offset + end - (links.Count - index) * ReferenceSize + WordOffset);
            var link = links[index];
            var target = doc.FindTable(link.Table);
            var kind = word switch
            {
                PressWord => ObjectEventKind.Press,
                ReleaseWord => ObjectEventKind.Release,
                ClickWord => state ? ObjectEventKind.Change : ObjectEventKind.Click,
                ChangeWord => state ? ObjectEventKind.Click : ObjectEventKind.Change,
                _ => (ObjectEventKind?)null
            };
            var read = word == FocusWord && target?.Name == "ADAPTDEMULTIPLE"
                ? Focus(doc, target, link.Index)
                : kind is { } eventKind && IsAction(target)
                    ? Action(doc, table, item, link, eventKind)
                    : null;
            if (read is not null)
            {
                events.TryAdd(read.Kind, read);
            }
        }
        return [.. events.Values];
    }

    /// <summary>Проверить назначение по целевой таблице: слово 0x8000 также встречается у ссылок на свойства кнопки.</summary>
    /// <returns>Признак действия или отсутствующей таблицы действия.</returns>
    private static bool IsAction(TocEntry? target) =>
        target is null || target.Name == "SECURITY_MONITO" || FunctionListReader.IsFunctionTable(target.Name);

    /// <summary>Прочитать Activate или Deactivate по значению посредника 1 или 0.</summary>
    /// <returns>Распознанное событие, даже если список повреждён; null, если вид определить нельзя.</returns>
    private static ObjectEvent? Focus(FwxDocument doc, TocEntry table, int index)
    {
        try
        {
            var (item, target, value) = FunctionListReader.DemultiplexedTarget(doc, table, index, "кнопка");
            var kind = value switch
            {
                1 => ObjectEventKind.Activate,
                0 => ObjectEventKind.Deactivate,
                _ => throw new FwxFormatException(table.Name, item.Offset,
                    $"запись {index + 1}: значение события кнопки {value}, ожидалось 0 или 1")
            };
            return Action(doc, table, item, target, kind);
        }
        catch (FwxFormatException error)
        {
            doc.Warnings.Add(error.Message);
            return null;
        }
    }

    /// <summary>Прочитать одно прямое событие, включая список под SECURITY_MONITO.</summary>
    /// <returns>Событие с вызовами или с признаком повреждения списка.</returns>
    private static ObjectEvent Action(FwxDocument doc, TocEntry table, TableItem item, VarReference link, ObjectEventKind kind)
    {
        try
        {
            var functions = FunctionListReader.AuthorizedFunctions(doc, table, item, link, $"запись {item.Index + 1}: {table.Name}");
            return new ObjectEvent(kind, Calls(doc, functions));
        }
        catch (FwxFormatException error)
        {
            doc.Warnings.Add(error.Message);
            return new ObjectEvent(kind, null);
        }
    }

    /// <summary>Прочитать вызовы с сохранением порядка и повторов, в том числе повреждённые вызовы.</summary>
    /// <returns>Вызовы в порядке списка.</returns>
    private static IReadOnlyList<FunctionCall> Calls(FwxDocument doc, IReadOnlyList<VarReference> functions) =>
        [.. functions.Select(f => FunctionReader.ReadOrUnread(doc, f, forButton: true))];
}
