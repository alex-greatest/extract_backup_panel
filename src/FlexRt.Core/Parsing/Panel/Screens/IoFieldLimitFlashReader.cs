using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Flash on limit violation I/O field: ссылка поля на ADAPTDYNATTRIBU, у её элемента — ссылка на SYMB, у элемента SYMB
/// первая ссылка — на LIMIT или VARLIST со служебным словом 0x10 (состояние пределов). Тогда цвета Limits лежат в SYMB,
/// а не в ADAPTVALUECHANG. Флажок задаёт бит 0x02 записи состояния; при Appearance цвета Limits могут быть в SYMB и без мигания.
/// (<see cref="IoFieldLimitReader"/>). Сверено с TIA на шагах <c>data/real/steps-iofield/</c> 79 (флажок снят) и 80
/// (установлен; Project1, V21, TP700 Comfort, тег DInt с Range): цвета 237,88,97 и 241,161,44 совпали с XML. Та же
/// цепочка с первой ссылкой SYMB на VAR — анимация Appearance (10 полей A603A0097), не мигание по пределам.
/// VARLIST, бит мигания и сочетание с Appearance подтверждены повтором в
/// <c>data/real/iofield-rendering-flashing-20261116/</c>, Output с прямым тегом — также в опытах 12–14.
/// </summary>
internal static class IoFieldLimitFlashReader
{
    /// <summary>Таблица динамических атрибутов: мигание по пределам и анимация Appearance.</summary>
    private const string DynamicTable = "ADAPTDYNATTRIBU";

    /// <summary>Таблица диапазонов с цветами.</summary>
    private const string RangesTable = "SYMB";

    /// <summary>Таблица пределов тега: первая ссылка SYMB на неё отличает мигание по пределам от анимации.</summary>
    private const string LimitTable = "LIMIT";

    /// <summary>Мультиплексированный тег; состояние пределов в ссылке SYMB отличает слово 0x10 от нуля у Appearance.</summary>
    private const string VarListTable = "VARLIST";

    /// <summary>Смещение первого служебного слова первой ссылки SYMB; подтверждено на Output с VARLIST, TIA V21.</summary>
    private const int ReferenceRoleOffset = 0x0c;

    /// <summary>Служебное слово ссылки SYMB на состояние пределов тега.</summary>
    private const int LimitStateRole = 0x10;

    /// <summary>Бит мигания в девятом байте записи цвета: 6 при установленном флажке, 4 при снятом с Appearance.</summary>
    private const int FlashFlag = 0x02;

    /// <summary>u16 число диапазонов N от конца ссылок SYMB; за ним u16 (наблюдение: всегда 1).</summary>
    private const int RangeCountOffset = 0;

    /// <summary>Начало диапазонов от конца ссылок: N пар (u32 от, u32 до).</summary>
    private const int RangesOffset = 4;

    /// <summary>Размер диапазона: u32 от, u32 до.</summary>
    private const int RangeSize = 8;

    /// <summary>
    /// Заголовок записей цветов за диапазонами — 4 байта: u8 (наблюдение: 0), u8 число записей (N + 1), u8 размер записи
    /// (наблюдение: 9), u8 (наблюдение: 0).
    /// </summary>
    private const int EntriesHeaderSize = 4;

    /// <summary>Наименьший размер записи цветов: два цвета RGBA и байт флагов.</summary>
    private const int MinEntrySize = 9;

    /// <summary>Байт флагов после двух цветов RGBA.</summary>
    private const int FlagsOffset = 8;

    /// <summary>Смещение цвета фона в записи: за цветом текста.</summary>
    private const int BackColorOffset = 4;

    /// <summary>Состояние «выше верхнего предела» (как ключ 4 в ADAPTVALUECHANG).</summary>
    private const long AboveUpperState = 4;

    /// <summary>Состояние «ниже нижнего предела» (как ключ 5 в ADAPTVALUECHANG).</summary>
    private const long BelowLowerState = 5;

    /// <summary>Цвета Limits мигания по пределам, если оно есть у поля.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="links">Ссылки элемента поля.</param>
    /// <param name="who">Чьё поле — для сообщения.</param>
    /// <returns>
    /// Цвета (состояния нет — цвет <c>null</c>) и флажок; у поля нет цепочки состояния пределов —
    /// <c>null</c>.
    /// </returns>
    /// <exception cref="FwxFormatException">Ссылка за последний элемент, элемент вне таблицы, диапазоны или цвета не помещаются.</exception>
    public static (LimitColors Colors, LimitFlashing Flash)? Read(FwxDocument doc, IReadOnlyList<VarReference> links, string who)
    {
        if (doc.FindTable(DynamicTable) is not { } dynamic || doc.FindTable(RangesTable) is not { } ranges)
        {
            return null;
        }
        foreach (var link in links.Where(l => l.Table == dynamic.Id))
        {
            var (_, dynamicLinks) = ReferenceHeader.Read(doc.Binary, ElementLink.Item(doc, dynamic, link.Index, who), dynamic.Name);
            foreach (var symb in dynamicLinks.Where(l => l.Table == ranges.Id))
            {
                var item = ElementLink.Item(doc, ranges, symb.Index, who);
                var (end, symbLinks) = ReferenceHeader.Read(doc.Binary, item, ranges.Name);
                if (symbLinks.Count > 0 && IsLimitState(doc, item, symbLinks[0]))
                {
                    return Colors(doc.Binary, item, end, ranges.Name);
                }
            }
        }
        return null;
    }

    /// <summary>LIMIT или VARLIST со словом состояния пределов; Appearance с VARLIST использует слово 0.</summary>
    /// <returns>Ссылка ведёт на состояние пределов, а не значение тега анимации.</returns>
    private static bool IsLimitState(FwxDocument doc, TableItem item, VarReference link) =>
        doc.FindTable(link.Table)?.Name switch
        {
            LimitTable => true,
            VarListTable => doc.Binary.D2(item.Offset + ReferenceRoleOffset) == LimitStateRole,
            _ => false
        };

    /// <summary>
    /// Цвета фона состояний 4 и 5: запись цветов k + 1 — для диапазона k, запись 0 — обычный вид (наблюдение: у мигания
    /// по пределам диапазоны 4, 0, 5; у анимации A603A0097 — свои).
    /// </summary>
    /// <returns>Цвета Limits и наличие бита мигания у состояний 4/5.</returns>
    /// <exception cref="FwxFormatException">Диапазоны или записи не помещаются, записей не N + 1 или запись короче 9 байт.</exception>
    private static (LimitColors Colors, LimitFlashing Flash) Colors(FwxBinary b, TableItem item, long end, string section)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (end + RangesOffset > item.Length)
        {
            throw new FwxFormatException(section, p, $"запись {number}: число диапазонов не помещается в {item.Length} байт");
        }
        var count = b.D2(p + end + RangeCountOffset);
        var header = end + RangesOffset + count * (long)RangeSize;
        if (header + EntriesHeaderSize > item.Length)
        {
            throw new FwxFormatException(section, p, $"запись {number}: {count} диапазонов не помещаются в {item.Length} байт");
        }
        var entries = b.D1(p + header + 1);
        var size = b.D1(p + header + 2);
        if (entries != count + 1 || size < MinEntrySize || header + EntriesHeaderSize + entries * (long)size > item.Length)
        {
            throw new FwxFormatException(section, p, $"запись {number}: {entries} записей цветов по {size} байт не сходятся с {count} диапазонами");
        }
        var ranges = p + end + RangesOffset;
        var colors = p + header + EntriesHeaderSize;
        var limits = new LimitColors(
            BackColor(b, ranges, colors, count, size, AboveUpperState),
            BackColor(b, ranges, colors, count, size, BelowLowerState));
        var flashing = Enumerable.Range(0, count)
            .Where(k => b.D4(ranges + k * (long)RangeSize) is AboveUpperState or BelowLowerState)
            .Any(k => (b.D1(colors + (k + 1L) * size + FlagsOffset) & FlashFlag) != 0);
        return (limits, flashing ? LimitFlashing.On : LimitFlashing.Off);
    }

    /// <summary>
    /// Цвет фона записи цветов диапазона, который начинается с состояния <paramref name="state"/> (сравнивается только
    /// u32 «от»; наблюдение: у мигания по пределам «от» и «до» равны).
    /// </summary>
    /// <param name="b">Байты файла.</param>
    /// <param name="ranges">Смещение первого диапазона в файле.</param>
    /// <param name="colors">Смещение первой записи цветов в файле.</param>
    /// <param name="count">Число диапазонов.</param>
    /// <param name="size">Размер записи цветов.</param>
    /// <param name="state">Состояние: 4 — выше верхнего предела, 5 — ниже нижнего.</param>
    /// <returns>Цвет <c>0xRRGGBB</c>; диапазона с таким состоянием нет — <c>null</c> (в окне «не прочитано»).</returns>
    private static int? BackColor(FwxBinary b, long ranges, long colors, int count, int size, long state) =>
        Enumerable.Range(0, count)
            .Where(k => b.D4(ranges + k * (long)RangeSize) == state)
            .Select(k => (int?)StyleElement.Rgb(b, colors + (k + 1) * (long)size + BackColorOffset))
            .FirstOrDefault();
}
