using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Tags;

/// <summary>Таблица тегов листа «Теги»: колонки как в HMI tags в TIA Portal.</summary>
public static class TagsTable
{
    /// <summary>Колонка имени тега: по ней окно находит строку тега I/O field.</summary>
    public const string NameColumn = "Name";

    /// <summary>Колонка типа данных тега (второе поле Address I/O field в окне).</summary>
    public const string DataTypeColumn = "Data type";

    /// <summary>Колонка тега ПЛК (PLC tag I/O field в окне).</summary>
    public const string PlcTagColumn = "PLC tag";

    /// <summary>Колонка адреса (Address I/O field в окне).</summary>
    public const string AddressColumn = "Address";

    /// <summary>Колонки — как в таблице HMI tags в TIA Portal.</summary>
    private static readonly string[] Columns =
    [
        NameColumn, DataTypeColumn, "Connection", "PLC name", PlcTagColumn, AddressColumn,
        "Access mode", "Acquisition cycle", "Logged", "Source comment"
    ];

    /// <summary>
    /// Построить таблицу: по строке на тег в порядке VAR, значения —
    /// <see cref="TagRowFormatter.Format"/>; пустое значение — пустая ячейка.
    /// </summary>
    /// <returns>Заголовки и строки тегов.</returns>
    public static ReportTable Build(FwxDocument doc, IReadOnlyList<PlcMatch?> matches) => new(Columns,
        [.. doc.Tags.Select((tag, i) => Row(doc, tag, i < matches.Count ? matches[i] : null))]);

    /// <summary>Строка тега: значения <see cref="TagRowFormatter.Format"/> ячейками.</summary>
    /// <returns>Ячейки строки.</returns>
    private static ReportCell[] Row(FwxDocument doc, HmiTag tag, PlcMatch? match) =>
        [.. TagRowFormatter.Format(doc, tag, match).Select(v => (ReportCell)v)];
}
