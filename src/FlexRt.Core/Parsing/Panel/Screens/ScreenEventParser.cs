using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel.Functions;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// События экранов Loaded и Cleared (вкладка Events экрана в TIA; сверено с TIA на копиях
/// <c>data/real/steps-events/</c> 09–24, Project1, V21, и на 13 событиях A603A0097): ссылка записи SCREEN на FUNC_LIST —
/// Loaded, на ADAPTDEMULTIPLE — Cleared; функции списка — элементы таблиц FUNC_* (<see cref="FunctionReader"/>).
/// </summary>
public static class ScreenEventParser
{
    /// <summary>Таблица, через которую экран ссылается на функции события Cleared.</summary>
    private const string ClearedTable = "ADAPTDEMULTIPLE";

    /// <summary>
    /// Прочитать события всех экранов (кроме служебных записей pop-up) и добавить в
    /// <see cref="FwxDocument.ScreenEvents"/> в порядке <see cref="FwxDocument.Screens"/>, Loaded перед Cleared; у экрана
    /// несколько ссылок на одну таблицу — первая. Разбирается после SCREEN. Нет таблиц FUNC_LIST и ADAPTDEMULTIPLE —
    /// событий нет, без сообщения. Сбой списка (FUNC_LIST, ADAPTDEMULTIPLE) — событие «не прочитано»
    /// (<see cref="ScreenEvent.Functions"/> — <c>null</c>); сбой одной функции — она «не прочитана»
    /// (<see cref="FunctionCall.Arguments"/> — <c>null</c>), остальные функции события читаются; в обоих случаях —
    /// предупреждение с таблицей и смещением (решение пользователя 2026-10-29).
    /// </summary>
    public static void Parse(FwxDocument doc)
    {
        var loaded = doc.FindTable(FunctionListReader.ListTable);
        var cleared = doc.FindTable(ClearedTable);
        foreach (var screen in doc.Screens.Where(s => !s.IsService))
        {
            Add(doc, screen, loaded, ScreenEventKind.Loaded, FunctionListReader.Loaded);
            Add(doc, screen, cleared, ScreenEventKind.Cleared, FunctionListReader.Cleared);
        }
    }

    /// <summary>Событие экрана по первой ссылке на таблицу события; ссылки нет — события нет.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="screen">Экран.</param>
    /// <param name="table">Таблица события; нет в TOC — ничего не делает.</param>
    /// <param name="kind">Событие.</param>
    /// <param name="read">Чтение списка функций события по номеру элемента таблицы (<see cref="FunctionListReader"/>).</param>
    private static void Add(
        FwxDocument doc,
        Screen screen,
        TocEntry? table,
        ScreenEventKind kind,
        Func<FwxDocument, TocEntry, Screen, int, IReadOnlyList<VarReference>> read)
    {
        if (table is null || screen.References.All(r => r.Table != table.Id))
        {
            return;
        }
        IReadOnlyList<VarReference> functions;
        try
        {
            functions = read(doc, table, screen, screen.References.First(r => r.Table == table.Id).Index);
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add(e.Message);
            doc.ScreenEvents.Add(new ScreenEvent(screen.Index, kind, null));
            return;
        }
        doc.ScreenEvents.Add(new ScreenEvent(screen.Index, kind, [.. functions.Select(f => FunctionReader.ReadOrUnread(doc, f))]));
    }
}
