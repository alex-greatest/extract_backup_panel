using FlexRt.Model.Matching;
using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData;

/// <summary>Статистика тегов панели для начала листа «Теги».</summary>
public static class TagStatistics
{
    /// <summary>
    /// Строки статистики: всего тегов; внутренних; связанных с ПЛК; из них найдено в ПЛК,
    /// отсутствует в файле ПЛК, данные ПЛК неизвестны (нет или сломан файл ПЛК, ПЛК не выбран,
    /// связь не разобрана); с абсолютной адресацией — все PLC-теги с абсолютным доступом,
    /// они могут входить и в найденные (тег ПЛК с тем же адресом). Найдено, отсутствует и
    /// неизвестно вместе с абсолютными без тега ПЛК дают число связанных с ПЛК.
    /// </summary>
    /// <returns>Подписи и числа по порядку строк листа.</returns>
    public static IReadOnlyList<(string Label, int Count)> Rows(FwxDocument doc, IReadOnlyList<PlcMatch?> matches)
    {
        var plc = doc.Tags.Select((tag, i) => (Tag: tag, Match: i < matches.Count ? matches[i] : null))
            .Where(t => t.Tag.LinkTable is not null)
            .ToList();
        var lookups = plc.Select(t => t.Match?.Link is null ? PlcLookup.Unknown : t.Match.Lookup).ToList();
        return
        [
            ("Всего тегов", doc.Tags.Count),
            ("Внутренних тегов", doc.Tags.Count - plc.Count),
            ("Тегов с ПЛК", plc.Count),
            ("Найдено в ПЛК", lookups.Count(l => l == PlcLookup.Found)),
            ("Есть в панели, нет в ПЛК", lookups.Count(l => l == PlcLookup.NotInPlcFile)),
            ("Данные ПЛК неизвестны", lookups.Count(l => l == PlcLookup.Unknown)),
            ("С абсолютной адресацией", plc.Count(t => t.Match?.Link?.Absolute == true))
        ];
    }
}
