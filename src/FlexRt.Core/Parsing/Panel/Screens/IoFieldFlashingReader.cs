using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Flashing I/O field: ссылка элемента на TIMER со словом <see cref="FlashingWord"/>. Сверено с TIA на шаге
/// <c>data/real/steps-iofield/50_flashing.fwc</c> (Input/output, Project1, V21); у поля Output шага нет — правило то же,
/// не проверено.
/// </summary>
internal static class IoFieldFlashingReader
{
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

    /// <summary>Есть ли у поля ссылка мигания на TIMER.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="item">Элемент поля.</param>
    /// <param name="body">Смещение конца ссылок от начала элемента.</param>
    /// <param name="links">Ссылки элемента в порядке файла.</param>
    /// <returns><c>true</c> — Flashing Standard, иначе Disabled.</returns>
    public static bool Read(FwxDocument doc, TableItem item, long body, IReadOnlyList<VarReference> links)
    {
        if (doc.FindTable(Table) is not { } timer)
        {
            return false;
        }
        var first = item.Offset + body - links.Count * ReferenceSize;
        return Enumerable.Range(0, links.Count)
            .Any(i => links[i].Table == timer.Id && doc.Binary.D2(first + i * ReferenceSize + WordOffset) == FlashingWord);
    }
}
