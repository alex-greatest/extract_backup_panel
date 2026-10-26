using System.Globalization;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Export.PanelData.Screens;

/// <summary>Колонки Visibility листа «Экраны»: тег, From, To и Visible/Invisible анимации Visibility экрана.</summary>
public sealed class ScreenVisibilityCells
{
    /// <summary>Заголовок колонки тега.</summary>
    public const string TagColumn = "Visibility tag";

    /// <summary>Заголовок колонки начала диапазона Range.</summary>
    public const string FromColumn = "Visibility from";

    /// <summary>Заголовок колонки конца диапазона Range.</summary>
    public const string ToColumn = "Visibility to";

    /// <summary>Заголовок колонки Visible/Invisible.</summary>
    public const string StateColumn = "Visibility";

    /// <summary>Значение колонки Visibility при Visible в TIA.</summary>
    public const string VisibleText = "Visible";

    /// <summary>Значение колонки Visibility при Invisible в TIA.</summary>
    public const string InvisibleText = "Invisible";

    /// <summary>Заголовки колонок в порядке листа.</summary>
    public static readonly string[] Columns = [TagColumn, FromColumn, ToColumn, StateColumn];

    /// <summary>Id таблицы LIMIT из TOC; таблицы нет — <c>null</c>.</summary>
    private readonly int? _limitTable;

    /// <summary>Id таблицы VAR из TOC; таблицы нет — <c>null</c>.</summary>
    private readonly int? _varTable;

    /// <summary>Visibility по номеру записи SCREEN (с нуля); у записи несколько — первая.</summary>
    private readonly Dictionary<int, ScreenVisibility> _byScreen;

    /// <summary>Имя тега по номеру записи VAR с единицы (<see cref="HmiTag.Index"/>).</summary>
    private readonly Dictionary<int, string> _tagNames;

    /// <summary>Собрать Id таблиц LIMIT и VAR, Visibility экранов и имена тегов документа один раз на таблицу листа.</summary>
    public ScreenVisibilityCells(FwxDocument doc)
    {
        _limitTable = doc.FindTable("LIMIT")?.Id;
        _varTable = doc.FindTable("VAR")?.Id;
        _byScreen = doc.ScreenVisibilities.DistinctBy(v => v.ScreenIndex).ToDictionary(v => v.ScreenIndex);
        _tagNames = doc.Tags.DistinctBy(t => t.Index).ToDictionary(t => t.Index, t => t.Name);
    }

    /// <summary>
    /// Ячейки Visibility экрана: у записи SCREEN нет ссылки на LIMIT — четыре пустые; ссылка есть, а элемент не прочитан
    /// (сбой LIMIT) — четыре <c>?</c>. Тег — имя записи VAR (ссылка не на VAR или записи нет — <c>?</c>); From и To —
    /// числом, дробное — текстом (тип значения не известен — <c>?</c>); Visibility — <c>Visible</c> или <c>Invisible</c>.
    /// </summary>
    /// <param name="screen">Запись SCREEN экрана.</param>
    /// <returns>Ячейки в порядке <see cref="Columns"/>.</returns>
    public ReportCell[] Of(Screen screen)
    {
        if (screen.References.All(r => r.Table != _limitTable))
        {
            return ["", "", "", ""];
        }
        if (!_byScreen.TryGetValue(screen.Index, out var visibility))
        {
            return [ScreensTable.Unknown, ScreensTable.Unknown, ScreensTable.Unknown, ScreensTable.Unknown];
        }
        return
        [
            TagName(visibility.Tag),
            ValueCell(visibility.From),
            ValueCell(visibility.To),
            visibility.Visible ? VisibleText : InvisibleText
        ];
    }

    /// <summary>Имя тега по ссылке: Id таблицы VAR из TOC и номер записи с нуля (<see cref="HmiTag.Index"/> — с единицы).</summary>
    /// <returns>Имя тега; ссылка не на VAR или запись не прочитана — <c>?</c>.</returns>
    private string TagName(VarReference tag) =>
        tag.Table == _varTable ? _tagNames.GetValueOrDefault(tag.Index + 1, ScreensTable.Unknown) : ScreensTable.Unknown;

    /// <summary>Ячейка From или To: целое — числом, дробное — текстом в инвариантной культуре.</summary>
    /// <returns>Ячейка; значение не прочитано — <c>?</c>.</returns>
    private static ReportCell ValueCell(double? value) => value switch
    {
        null => ScreensTable.Unknown,
        { } v when double.IsInteger(v) && v is >= long.MinValue and <= long.MaxValue => (long)v,
        { } v => v.ToString("R", CultureInfo.InvariantCulture)
    };
}
