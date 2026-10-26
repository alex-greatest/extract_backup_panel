using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Разбор LIMIT: Visibility экранов. Читаются только элементы, на которые ссылаются записи SCREEN: в таблице есть и
/// элементы другой раскладки (с именем, как запись VAR, — наблюдение на A603A0097), они не разбираются.
/// </summary>
public static class LimitParser
{
    /// <summary>Первое слово элемента без имени (у элемента с именем — u16 3 и имя, такие не читаются).</summary>
    private const int AnonymousMarker = 1;

    /// <summary>
    /// Смещение байта флагов от конца ссылок: за ссылками 8 байт <c>03 7c 03 00 00 00 00 00</c> (Visible) или
    /// <c>03 74 03 00 …</c> (Invisible); остальные биты и байты не разобраны (на шагах 01–08 не менялись).
    /// </summary>
    private const int FlagsOffset = 0x01;

    /// <summary>Бит Visible в байте флагов (сверено с TIA: шаг 03, Visible → Invisible, <c>0x7c</c> → <c>0x74</c>).</summary>
    private const int VisibleBit = 0x08;

    /// <summary>Смещение From от конца ссылок: 16 байт, как VARIANT — u16 тип, 6 байт, 8 байт значения.</summary>
    private const int FromOffset = 0x08;

    /// <summary>Смещение To от конца ссылок: 16 байт, как у From.</summary>
    private const int ToOffset = 0x18;

    /// <summary>Конец To от конца ссылок: столько байт должно быть за ссылками.</summary>
    private const int ValuesEnd = 0x28;

    /// <summary>Смещение значения в 16-байтовом блоке (за u16 типом и 6 байтами).</summary>
    private const int ValueOffset = 0x08;

    /// <summary>Тип значения VARIANT <c>VT_R8</c>: f64 (у всех Visibility экранов, сверено на шагах 01–08).</summary>
    private const int DoubleType = 5;

    /// <summary>Тип значения VARIANT <c>VT_I2</c>: i16 (наблюдение: в элементах LIMIT A603A0097, на которые ссылается не экран).</summary>
    private const int Int16Type = 2;

    /// <summary>
    /// Прочитать элементы LIMIT, на которые ссылаются записи SCREEN (ссылка записи с Id таблицы LIMIT из TOC; второе
    /// слово ссылки — 0x10, наблюдение), и добавить их в <see cref="FwxDocument.ScreenVisibilities"/>. Нет таблицы в TOC — ничего не делает.
    /// Разбирается после SCREEN. Значение From или To незнакомого типа — <c>null</c> и предупреждение, элемент остаётся.
    /// Сбой элемента одного экрана (ссылка за последний элемент, элемент за пределами таблицы или не той раскладки) —
    /// предупреждение с таблицей и смещением, у этого экрана Visibility нет, остальные экраны читаются; элементы, на
    /// которые экраны не ссылаются, не проверяются (их проверяет <see cref="TableLayoutCheck"/>).
    /// </summary>
    /// <exception cref="FwxFormatException">Каталог смещений таблицы выходит за файл или даёт отрицательную длину элемента.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("LIMIT");
        if (table is null)
        {
            return;
        }

        var items = FwxReader.Items(doc.Binary, table).ToList();
        foreach (var screen in doc.Screens.Where(s => !s.IsService))
        {
            foreach (var link in screen.References.Where(r => r.Table == table.Id))
            {
                try
                {
                    doc.ScreenVisibilities.Add(ReadLinked(doc, table, items, screen, link.Index));
                }
                catch (FwxFormatException e)
                {
                    // сбой одного элемента не должен прятать Visibility остальных экранов
                    doc.Warnings.Add(e.Message);
                }
            }
        }
    }

    /// <summary>Элемент LIMIT, на который ссылается экран: проверить номер и границы элемента и прочитать его.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица LIMIT.</param>
    /// <param name="items">Элементы таблицы в порядке каталога.</param>
    /// <param name="screen">Запись SCREEN, которая ссылается на элемент.</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <returns>Visibility экрана.</returns>
    /// <exception cref="FwxFormatException">
    /// Ссылка ведёт за последний элемент, элемент выходит за пределы таблицы или не сходится с раскладкой.
    /// </exception>
    private static ScreenVisibility ReadLinked(FwxDocument doc, TocEntry table, List<TableItem> items, Screen screen, int index)
    {
        if (index >= items.Count)
        {
            throw new FwxFormatException(table.Name, table.Offset, $"экран {screen.Name} ссылается на элемент {index + 1}, а элементов {table.Entries}");
        }
        var item = items[index];
        FwxReader.CheckInsideTable(table, item);
        return ReadRecord(doc, screen.Index, item);
    }

    /// <summary>
    /// Элемент без имени (сверено с TIA на шагах 01–08): начало со ссылками (<see cref="ReferenceHeader"/>; первая
    /// ссылка — тег: u16 Id таблицы, u16 номер записи), 8 байт флагов (на +0x01 бит Visible), From и To по 16 байт.
    /// </summary>
    /// <returns>Visibility экрана.</returns>
    /// <exception cref="FwxFormatException">
    /// Элемент короче 8 байт, первое слово не 1, R равно 0 или больше 0x20, ссылки, флаги или значения не помещаются в элемент.
    /// </exception>
    private static ScreenVisibility ReadRecord(FwxDocument doc, int screenIndex, TableItem item)
    {
        var b = doc.Binary;
        var p = item.Offset;
        var number = item.Index + 1;
        var (end, links) = ReferenceHeader.Read(b, item);
        var marker = b.D4(p);
        if (marker != AnonymousMarker)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: первое слово {marker}, ожидалось {AnonymousMarker}");
        }
        if (links.Count == 0 || end + ValuesEnd > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: {links.Count} ссылок и значения не помещаются в {item.Length} байт");
        }
        var tag = links[0];
        var visible = (b.D1(p + end + FlagsOffset) & VisibleBit) != 0;
        var from = Value(doc, item, p + end + FromOffset, "From");
        var to = Value(doc, item, p + end + ToOffset, "To");
        return new ScreenVisibility(screenIndex, tag, visible, from, to);
    }

    /// <summary>
    /// Значение блока VARIANT на <paramref name="pos"/>: тип <see cref="DoubleType"/> — f64, <see cref="Int16Type"/> — i16;
    /// другой тип — предупреждение с номером записи и смещением.
    /// </summary>
    /// <param name="doc">Документ: в него пишется предупреждение.</param>
    /// <param name="item">Элемент таблицы — для предупреждения.</param>
    /// <param name="pos">Смещение блока от начала файла.</param>
    /// <param name="what">Какое значение (From или To) — для предупреждения.</param>
    /// <returns>Значение; тип не известен — <c>null</c>.</returns>
    private static double? Value(FwxDocument doc, TableItem item, long pos, string what)
    {
        var b = doc.Binary;
        var type = b.D2(pos);
        switch (type)
        {
            case DoubleType:
                return BitConverter.Int64BitsToDouble(b.D4(pos + ValueOffset) | b.D4(pos + ValueOffset + 4) << 32);
            case Int16Type:
                return (short)b.D2(pos + ValueOffset);
            default:
                doc.Warnings.Add($"{b.Section}: запись {item.Index + 1} @ 0x{item.Offset:X}: тип значения {what} 0x{type:x} не известен, {what} не прочитано");
                return null;
        }
    }
}
