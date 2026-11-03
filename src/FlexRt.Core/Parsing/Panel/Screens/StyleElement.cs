using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Элемент таблицы стиля объекта (PS_BorderedShap, PS_Text, PS_Fill_Color) или FONT по ссылке из элемента объекта: элемент
/// по номеру с проверкой длины; таблицу по Id ссылки и её имя проверяет вызывающий.
/// </summary>
internal static class StyleElement
{
    /// <summary>Элемент таблицы по номеру из ссылки; элемент — не короче <paramref name="minLength"/>.</summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица стиля (имя проверено вызывающим).</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="minLength">Наименьшая длина элемента: последнее читаемое поле помещается.</param>
    /// <param name="owner">Чья ссылка — для сообщения (<c>объект I/O field_1</c>).</param>
    /// <returns>Номер, начало и длина элемента.</returns>
    /// <exception cref="FwxFormatException">Ссылка за последним элементом, элемент вне таблицы или короче <paramref name="minLength"/>.</exception>
    public static TableItem Item(FwxDocument doc, TocEntry table, int index, int minLength, string owner)
    {
        var item = ElementLink.Item(doc, table, index, owner);
        return item.Length >= minLength
            ? item
            : throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: длина {item.Length} меньше 0x{minLength:x}");
    }

    /// <summary>Цвет из трёх байтов R, G, B подряд (наблюдение: за ними нулевой байт); так же — цвет фона записи SCREEN.</summary>
    /// <returns>Цвет <c>0xRRGGBB</c>.</returns>
    public static int Rgb(FwxBinary b, long position) => b.D1(position) << 16 | b.D1(position + 1) << 8 | b.D1(position + 2);
}
