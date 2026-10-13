using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Tags;

/// <summary>Таблица тегов листа «Теги»: колонки как в HMI tags в TIA Portal.</summary>
public static class TagsTable
{
    /// <summary>Колонки — как в таблице HMI tags в TIA Portal.</summary>
    private static readonly string[] Columns =
    [
        "Name", "Data type", "Connection", "PLC name", "PLC tag", "Address",
        "Access mode", "Acquisition cycle", "Logged", "Source comment"
    ];

    /// <summary>
    /// Построить таблицу: по строке на тег в порядке VAR, значения —
    /// <see cref="TagRowFormatter.Format"/>; пустое значение — пустая ячейка.
    /// </summary>
    /// <returns>Заголовки и строки тегов.</returns>
    public static ReportTable Build(FwxDocument doc, IReadOnlyList<PlcMatch?> matches) => new(Columns,
        doc.Tags.Select((tag, i) => TagRowFormatter.Format(doc, tag, i < matches.Count ? matches[i] : null)
            .Select(v => (ReportCell)v).ToArray()).ToList());
}
