using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// I/O field: элементы VIEW_IN и VIEW_OUT объектов экранов (<see cref="FwxDocument.ScreenObjects"/>) с кодом I/O field
/// за ссылками. Сверено с TIA на шаговых копиях <c>data/real/steps-iofield/</c> 00–36 (Project1, V21, TP700 Comfort,
/// одно свойство на шаг, XML шага от Openness рядом); код типа — ещё на 477 объектах VIEW_IN/VIEW_OUT A603A0097 по
/// именам объектов.
/// </summary>
public static class IoFieldParser
{
    /// <summary>Таблица полей Input и Input/output.</summary>
    private const string InTable = "VIEW_IN";

    /// <summary>Таблица полей Output (в ней же Text field и Symbolic I/O field — с другим кодом типа).</summary>
    private const string OutTable = "VIEW_OUT";

    /// <summary>
    /// u32 код типа объекта от конца ссылок: <see cref="InKind"/>, <see cref="OutKind"/>; у Text field
    /// <c>0x98C000</c>, у Symbolic I/O field <c>0x88C000</c> (наблюдение: 477 объектов A603A0097 и копии Project1).
    /// </summary>
    private const int KindOffset = 0x10;

    /// <summary>Код I/O field в VIEW_IN.</summary>
    private const long InKind = 0x5DC000;

    /// <summary>Код I/O field в VIEW_OUT.</summary>
    private const long OutKind = 0x5CC000;

    /// <summary>
    /// u32 размер блока от <see cref="BlockOffset"/> до конца ссылок на стили (растёт с длиной Units; наблюдение на
    /// Project1 и A603A0097).
    /// </summary>
    private const int BlockSizeOffset = 0x14;

    /// <summary>Начало блока, размер которого — <see cref="BlockSizeOffset"/>.</summary>
    private const int BlockOffset = 0x18;

    /// <summary>
    /// Метка за ссылками на стили, выровненная до кратности 4 (наблюдение: всегда <c>00 FE FF 9F</c>). Ссылки — три по
    /// 4 байта в конце блока (<see cref="BlockSizeOffset"/>), сразу за текстом Units, поэтому бывают не выровнены:
    /// u16 Id таблицы (PS_BorderedShap, PS_Text, PS_Fill_Color), u16 номер элемента. За меткой — ссылка на FONT
    /// (u16 номер элемента, u16 Id таблицы).
    /// </summary>
    private const long StyleMark = 0x9FFFFE00;

    /// <summary>Число ссылок на стили перед <see cref="StyleMark"/>.</summary>
    private const int StyleCount = 3;

    /// <summary>u32 код формата (Display format).</summary>
    private const int FormatOffset = 0x20;

    /// <summary>u16 знаков после запятой (за ним u16, наблюдение: всегда 3).</summary>
    private const int DecimalsOffset = 0x24;

    /// <summary>У VIEW_IN: u16 режим — <c>0xFFFF</c> Input/output, 0 — Input; за ним поля формата.</summary>
    private const int InModeOffset = 0x2c;

    /// <summary>Начало полей формата у VIEW_IN: за словом режима.</summary>
    private const int InPatternOffset = 0x2e;

    /// <summary>Начало полей формата у VIEW_OUT: слова режима нет.</summary>
    private const int OutPatternOffset = 0x2c;

    /// <summary>u16 знак <c>s</c> в шаблоне (<c>0xFFFF</c>) от начала полей формата; на +0 — u16 длина шаблона.</summary>
    private const int SignedOffset = 4;

    /// <summary>u16 Leading zeros (<c>0xFFFF</c>) от начала полей формата.</summary>
    private const int LeadingZerosOffset = 8;

    /// <summary>
    /// u16 длина Units в байтах вместе с нулём от начала полей формата у VIEW_IN; перед ним u16 (наблюдение: 0;
    /// гипотеза — Hidden input).
    /// </summary>
    private const int InUnitOffset = 0x0c;

    /// <summary>u16 длина Units в байтах вместе с нулём от начала полей формата у VIEW_OUT.</summary>
    private const int OutUnitOffset = 0x0a;

    /// <summary>Текст Units UTF-16LE от поля его длины: за длиной u16 (наблюдение: 0).</summary>
    private const int UnitTextOffset = 4;

    /// <summary>
    /// Прочитать I/O field всех объектов экранов в <see cref="FwxDocument.IoFields"/>. Разбирается после
    /// <see cref="ScreenObjectParser"/>; элемент читается один раз (объект шаблона — на многих экранах). Элемент VIEW_IN
    /// или VIEW_OUT с другим кодом типа (Text field и т. п.) пропускается без сообщения. Сбой элемента поля или его
    /// стиля — предупреждение с таблицей и смещением, поля нет (объект остаётся рамкой).
    /// </summary>
    public static void Parse(FwxDocument doc)
    {
        foreach (var o in doc.ScreenObjects.Where(o => o.Table is InTable or OutTable).DistinctBy(o => o.Element))
        {
            try
            {
                if (ReadRecord(doc, o) is { } field)
                {
                    doc.IoFields.Add(field);
                }
            }
            catch (FwxFormatException e)
            {
                doc.Warnings.Add(e.Message);
            }
        }
    }

    /// <summary>Прочитать элемент объекта: I/O field или <c>null</c>, если код типа другой.</summary>
    /// <returns>Поле; не I/O field — <c>null</c>.</returns>
    /// <exception cref="FwxFormatException">Элемент или его стиль не сходятся.</exception>
    private static IoField? ReadRecord(FwxDocument doc, ScreenObject o)
    {
        var table = doc.FindTable(o.Element.Table)!;
        var item = ElementLink.Item(doc, table, o.Element.Index);
        var b = doc.Binary;
        var body = NamedElement.TryBodyOffset(b, item);
        if (body is not { } e || e + BlockOffset > item.Length)
        {
            return null;
        }
        var kind = b.D4(item.Offset + e + KindOffset);
        var isIn = o.Table == InTable;
        if (kind != (isIn ? InKind : OutKind))
        {
            return null;
        }
        var who = $"запись {item.Index + 1}: объект {o.Name}";
        var pattern = e + (isIn ? InPatternOffset : OutPatternOffset);
        var unit = pattern + (isIn ? InUnitOffset : OutUnitOffset);
        var stylesEnd = e + BlockOffset + b.D4(item.Offset + e + BlockSizeOffset);
        var styles = stylesEnd - StyleCount * 4;
        var mark = (stylesEnd + 3) / 4 * 4;
        if (unit + 2 > styles || mark + 8 > item.Length || b.D4(item.Offset + mark) != StyleMark)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"{who}: метка стилей I/O field не на месте");
        }
        var p = item.Offset;
        var tag = IoFieldTagReader.Read(doc, item);
        return new IoField(
            o.Element,
            !isIn ? IoFieldMode.Output : b.D2(p + e + InModeOffset) != 0 ? IoFieldMode.InputOutput : IoFieldMode.Input,
            tag.Name,
            tag.Via,
            tag.Type,
            (int)b.D4(p + e + FormatOffset),
            b.D2(p + e + DecimalsOffset),
            b.D2(p + pattern),
            b.D2(p + pattern + SignedOffset) != 0,
            b.D2(p + pattern + LeadingZerosOffset) != 0,
            Unit(b, table, item, unit, styles, who),
            Read(doc, table, item, styles, BorderStyleReader.Table, who, BorderStyleReader.Read),
            Read(doc, table, item, styles, TextStyleReader.Table, who, TextStyleReader.Read),
            Read(doc, table, item, styles, FillStyleReader.Table, who, FillStyleReader.Read),
            Font(doc, table, item, mark, who));
    }

    /// <summary>Units: u16 длина в байтах вместе с нулём, u16, текст UTF-16LE.</summary>
    /// <returns>Текст; длина 2 (только ноль) — пустая строка.</returns>
    /// <exception cref="FwxFormatException">Длина нечётная, меньше 2 или текст заходит на ссылки на стили.</exception>
    private static string Unit(FwxBinary b, TocEntry table, TableItem item, long unit, long end, string who)
    {
        var bytes = b.D2(item.Offset + unit);
        if (bytes < 2 || bytes % 2 != 0 || unit + UnitTextOffset + bytes > end)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"{who}: длина Units {bytes} байт не сходится");
        }
        return b.GetNameLen(item.Offset + unit + UnitTextOffset, bytes / 2 - 1);
    }

    /// <summary>
    /// Прочитать стиль: среди трёх ссылок с <paramref name="styles"/> — та, что ведёт в таблицу с этим именем (по Id таблицы из TOC),
    /// элемент читает <paramref name="read"/>.
    /// </summary>
    /// <returns>Стиль.</returns>
    /// <exception cref="FwxFormatException">Ни одна ссылка не ведёт в таблицу с этим именем или элемент стиля не сходится.</exception>
    private static T Read<T>(
        FwxDocument doc,
        TocEntry table,
        TableItem item,
        long styles,
        string name,
        string who,
        Func<FwxDocument, TocEntry, int, string, T> read)
    {
        foreach (var link in StyleLinks(doc, item, styles))
        {
            if (doc.FindTable(link.Table) is { } style && style.Name == name)
            {
                return read(doc, style, link.Index, who);
            }
        }
        throw new FwxFormatException(table.Name, item.Offset, $"{who}: нет ссылки на {name}");
    }

    /// <summary>Три ссылки на стили с <paramref name="styles"/> (от начала элемента): u16 Id таблицы, u16 номер элемента.</summary>
    /// <returns>Ссылки в порядке файла.</returns>
    private static IEnumerable<VarReference> StyleLinks(FwxDocument doc, TableItem item, long styles) =>
        Enumerable.Range(0, StyleCount)
            .Select(i => item.Offset + styles + i * 4L)
            .Select(p => new VarReference(doc.Binary.D2(p), doc.Binary.D2(p + 2)));

    /// <summary>Шрифт: ссылка за меткой — u16 номер элемента, u16 Id таблицы FONT.</summary>
    /// <returns>Шрифт.</returns>
    /// <exception cref="FwxFormatException">Ссылка не на FONT или элемент шрифта не сходится.</exception>
    private static PanelFont Font(FwxDocument doc, TocEntry table, TableItem item, long mark, string who)
    {
        var p = item.Offset + mark + 4;
        var font = doc.FindTable(doc.Binary.D2(p + 2));
        return font?.Name == FontReader.Table
            ? FontReader.Read(doc, font, doc.Binary.D2(p), who)
            : throw new FwxFormatException(table.Name, item.Offset, $"{who}: ссылка за стилями ведёт не на {FontReader.Table}");
    }
}
