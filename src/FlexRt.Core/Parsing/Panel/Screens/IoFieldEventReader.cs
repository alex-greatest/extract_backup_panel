using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel.Functions;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// События I/O field (вкладка Events объекта; сверено с TIA на копиях <c>data/real/steps-iofield/</c> 82–85, Project1,
/// V21, TP700 Comfort, и на поле A603A0097): ссылка поля на ADAPTDEMULTIPLE — Activate (значение 1) или Deactivate
/// (0), функции — по ссылке элемента (FUNC_LIST или одна FUNC_*); ссылка со вторым словом <c>0x8141</c> —
/// OnFinishInput: на функцию или FUNC_LIST, при Authorization — на SECURITY_MONITO, у которого ссылка на SECURITY_UNIT
/// и ссылка на функцию или список.
/// </summary>
internal static class IoFieldEventReader
{
    /// <summary>Таблица событий Activate и Deactivate.</summary>
    private const string DemultiplexerTable = "ADAPTDEMULTIPLE";

    /// <summary>Второе слово ссылки поля на функции OnFinishInput (у ссылки на запись тега и у остальных — другое; шаги 84, 85).</summary>
    private const int FinishInputWord = 0x8141;

    /// <summary>Значение ADAPTDEMULTIPLE у Activate.</summary>
    private const long ActivateValue = 1;

    /// <summary>Значение ADAPTDEMULTIPLE у Deactivate.</summary>
    private const long DeactivateValue = 0;

    /// <summary>
    /// Прочитать события поля: на событие — первая ссылка. Сбой ADAPTDEMULTIPLE (в том числе значение не 0 и не 1) —
    /// предупреждение, события нет (не известно, Activate это или Deactivate); сбой OnFinishInput — предупреждение,
    /// событие «не прочитано»; сбой одной функции — она «не прочитана», остальные читаются.
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица поля (VIEW_IN, VIEW_OUT).</param>
    /// <param name="item">Элемент поля.</param>
    /// <param name="end">Смещение конца ссылок поля от начала элемента.</param>
    /// <param name="links">Ссылки поля в порядке файла.</param>
    /// <param name="who">Чьи ссылки — для сообщений (<c>запись 3: объект F01</c>).</param>
    /// <returns>События по порядку <see cref="ObjectEventKind"/>; событий нет — пусто.</returns>
    public static IReadOnlyList<ObjectEvent> Read(FwxDocument doc, TocEntry table, TableItem item, long end, IReadOnlyList<VarReference> links, string who)
    {
        var events = new Dictionary<ObjectEventKind, ObjectEvent>();
        for (var k = 0; k < links.Count; k++)
        {
            var link = links[k];
            if (doc.FindTable(link.Table) is not { } target)
            {
                continue;
            }
            var word = ReferenceHeader.ReadSecondWord(doc.Binary, item.Offset + end, links.Count - k);
            var read = target.Name == DemultiplexerTable ? Demultiplexed(doc, target, link, who)
                : word == FinishInputWord ? FinishInput(doc, table, item, link, who)
                : null;
            if (read is not null)
            {
                events.TryAdd(read.Kind, read);
            }
        }
        return [.. events.Values.OrderBy(e => e.Kind)];
    }

    /// <summary>Activate или Deactivate по элементу ADAPTDEMULTIPLE.</summary>
    /// <returns>Событие; элемент не сходится — <c>null</c> и предупреждение.</returns>
    private static ObjectEvent? Demultiplexed(FwxDocument doc, TocEntry target, VarReference link, string who)
    {
        try
        {
            var (functions, value) = FunctionListReader.Demultiplexed(doc, target, link.Index, who);
            var kind = value switch
            {
                ActivateValue => ObjectEventKind.Activate,
                DeactivateValue => ObjectEventKind.Deactivate,
                _ => throw new FwxFormatException(target.Name, ElementLink.Item(doc, target, link.Index).Offset,
                    $"запись {link.Index + 1}: значение {value} не 0 и не 1 ({who}: событие не прочитано)")
            };
            return new ObjectEvent(kind, FunctionReader.ReadCalls(doc, functions).ToList());
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add(e.Message);
            return null;
        }
    }

    /// <summary>OnFinishInput: функции по ссылке поля, через SECURITY_MONITO — по его ссылке не на SECURITY_UNIT.</summary>
    /// <returns>Событие; не прочитано — функции <c>null</c> и предупреждение.</returns>
    private static ObjectEvent FinishInput(FwxDocument doc, TocEntry table, TableItem item, VarReference link, string who)
    {
        try
        {
            var functions = FunctionListReader.AuthorizedFunctions(doc, table, item, link, who, $" ({who})");
            return new ObjectEvent(ObjectEventKind.OnFinishInput, FunctionReader.ReadCalls(doc, functions).ToList());
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add(e.Message);
            return new ObjectEvent(ObjectEventKind.OnFinishInput, null);
        }
    }
}
