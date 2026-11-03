using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Цвета Limits I/O field: список <see cref="ValueChangeList"/> вида 3 в элементе ADAPTVALUECHANG, на который ссылается
/// поле. Сверено с TIA на шаговых копиях <c>data/real/steps-iofield/</c> 66–67 (Project1, V21, Range у тега DInt; цвета
/// Limits и фона — из XML шага) и на A603A0097 (9 полей: 7 с Range у тега и <c>IO Field_2</c> на двух экранах — тег через
/// VARLIST, цвета 255,0,0 и 255,255,0). Без Range у тега ссылки нет (шаг 65).
/// </summary>
internal static class IoFieldLimitReader
{
    /// <summary>Вид списка с цветами (наблюдение: у всех 11 полей со ссылкой на ADAPTVALUECHANG).</summary>
    private const long ColorsKind = 3;

    /// <summary>Размер пары: u32 ключ, R, G, B, 0.</summary>
    private const int PairSize = 8;

    /// <summary>Смещение цвета R, G, B в паре: за u32 ключом.</summary>
    private const int ColorOffset = 4;

    /// <summary>Ключ цвета выше верхнего предела. Ключ 0 — цвет фона поля (шаг 67), не читается.</summary>
    private const long AboveUpperKey = 4;

    /// <summary>Ключ цвета ниже нижнего предела.</summary>
    private const long BelowLowerKey = 5;

    /// <summary>
    /// Цвета Limits по первой ссылке элемента поля на ADAPTVALUECHANG (у поля с тегом через VARLIST это та же ссылка, по
    /// которой <see cref="IoFieldTagReader"/> ищет тег).
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="links">Ссылки элемента поля.</param>
    /// <param name="who">Чьё поле — для сообщения.</param>
    /// <returns>
    /// Цвета; ссылки на ADAPTVALUECHANG нет или список другого вида (не цвета) — <c>null</c>. Ключа нет — цвет <c>null</c>.
    /// </returns>
    /// <exception cref="FwxFormatException">Ссылка за последний элемент, элемент вне таблицы или пары не помещаются.</exception>
    public static LimitColors? Read(FwxDocument doc, IReadOnlyList<VarReference> links, string who)
    {
        if (doc.FindTable(ValueChangeList.Table) is not { } table)
        {
            return null;
        }
        var link = links.ToList().FindIndex(r => r.Table == table.Id);
        if (link < 0)
        {
            return null;
        }
        var element = ElementLink.Item(doc, table, links[link].Index, who);
        var b = doc.Binary;
        var (end, _) = ReferenceHeader.Read(b, element, table.Name);
        // список другого вида — не цвета, его пары другого размера и не проверяются
        if (ValueChangeList.TryKind(b, element, end) is { } kind && kind != ColorsKind)
        {
            return null;
        }
        var (pairs, count, _) = ValueChangeList.Read(b, element, end, PairSize, table.Name);
        var offsets = Enumerable.Range(0, count).Select(i => pairs + i * PairSize).ToList();
        return new LimitColors(Color(b, offsets, AboveUpperKey), Color(b, offsets, BelowLowerKey));
    }

    /// <summary>Цвет пары с ключом <paramref name="key"/>: u32 ключ, за ним R, G, B.</summary>
    /// <returns>Цвет <c>0xRRGGBB</c>; пары с таким ключом нет — <c>null</c>.</returns>
    private static int? Color(FwxBinary b, IEnumerable<long> pairs, long key) =>
        pairs.Where(q => b.D4(q) == key).Select(q => (int?)StyleElement.Rgb(b, q + ColorOffset)).FirstOrDefault();
}
