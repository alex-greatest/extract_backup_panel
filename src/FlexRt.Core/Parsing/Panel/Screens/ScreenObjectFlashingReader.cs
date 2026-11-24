using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Flashing: ссылка на TIMER со словом <see cref="FlashingWord"/>. Для I/O field сверено с TIA на шаге
/// <c>data/real/steps-iofield/50_flashing.fwc</c> (Input/output, Project1, V21) и на Output
/// <c>data/real/iofield-rendering-flashing-20261116/ui/05_output_standard.fwc</c> (TP700 Comfort, TIA V21 Update 1).
/// </summary>
internal static class ScreenObjectFlashingReader
{
    /// <summary>Промежуточная таблица мигания кнопок (TP700 Comfort, V21).</summary>
    private const string ButtonAdapterTable = "ADAPTCYCLICCHAN";

    /// <summary>Таблица таймеров.</summary>
    private const string Table = "TIMER";

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Смещение второго слова в ссылке.</summary>
    private const int WordOffset = 6;

    /// <summary>
    /// Второе слово ссылки на TIMER при Flashing Standard (шаг 50). Другое значение — не мигание: у служебных объектов
    /// <c>HmiScreenItemData</c> VIEW_OUT слово 55, а Flashing в TIA у всех полей A603A0097 — Disabled.
    /// </summary>
    private const int FlashingWord = 105;

    /// <summary>Найти ссылку мигания на TIMER в заголовке поля или адаптера кнопки.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="item">Элемент со ссылками.</param>
    /// <param name="body">Смещение конца ссылок от начала элемента.</param>
    /// <param name="links">Ссылки элемента в порядке файла.</param>
    /// <returns><c>true</c> — Flashing Standard, иначе Disabled.</returns>
    public static bool HasStandardTimer(FwxDocument doc, TableItem item, long body, IReadOnlyList<VarReference> links)
    {
        if (doc.FindTable(Table) is not { } timer)
        {
            return false;
        }
        var first = item.Offset + body - links.Count * ReferenceSize;
        return Enumerable.Range(0, links.Count)
            .Any(i => links[i].Table == timer.Id && doc.Binary.D2(first + i * ReferenceSize + WordOffset) == FlashingWord);
    }

    /// <summary>Найти Standard по адаптеру. Ошибка адаптера не скрывает остальные свойства кнопки.</summary>
    /// <returns>Standard, Disabled или null, если ссылка адаптера повреждена.</returns>
    public static bool? ReadButton(FwxDocument doc, IReadOnlyList<VarReference> links)
    {
        if (doc.FindTable(ButtonAdapterTable) is not { } adapters)
        {
            return false;
        }
        try
        {
            foreach (var link in links.Where(link => link.Table == adapters.Id))
            {
                var item = ElementLink.Item(doc, adapters, link.Index);
                var (end, references) = ReferenceHeader.Read(doc.Binary, item, adapters.Name);
                if (HasStandardTimer(doc, item, end, references))
                {
                    return true;
                }
            }
            return false;
        }
        catch (FwxFormatException error)
        {
            if (!doc.Warnings.Contains(error.Message))
            {
                doc.Warnings.Add(error.Message);
            }
            return null;
        }
    }
}
