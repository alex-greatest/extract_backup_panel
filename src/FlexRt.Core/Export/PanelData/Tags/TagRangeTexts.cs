using System.Globalization;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Tags;

/// <summary>Тексты Range в свойствах HMI-тега; отсутствие LIMIT не доказывает, что Range не задан в проекте.</summary>
public static class TagRangeTexts
{
    /// <summary>Число, имя тега, выключенная граница, отсутствие данных или сбой чтения.</summary>
    /// <returns>Значение для просмотра.</returns>
    public static string Value(TagRange? range, TagRangeLimit? limit) => range switch
    {
        null => "нет данных в FWC",
        { IsRead: false } => "не прочитано",
        _ => limit?.Tag ?? limit?.Value?.ToString("G", CultureInfo.InvariantCulture) ?? "не задано"
    };

    /// <summary>Тип прочитанной границы; выключенная или не прочитанная граница не получает выдуманный тип.</summary>
    /// <returns>Const, Tag или null.</returns>
    public static ReportCell? Kind(TagRange? range, TagRangeLimit? limit) => range is { IsRead: true } && limit is not null
        ? (ReportCell)(limit.Tag is null ? "Const" : "Tag")
        : (ReportCell?)null;
}
