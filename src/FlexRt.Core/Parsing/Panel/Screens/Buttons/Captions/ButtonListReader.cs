using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens.Buttons.Captions;

/// <summary>Списки General кнопки: связь роли 0x52 с SYMB, прямой VAR и десятичные диапазоны.</summary>
internal static class ButtonListReader
{
    /// <summary>Роль ссылки кнопки на список подписей.</summary>
    private const int ListRole = 0x52;

    /// <summary>Ссылка: u16 id таблицы, u16 индекс, u16 роль и u16 неизвестного назначения.</summary>
    private const int LinkSize = 8;

    /// <summary>Смещение роли от начала ссылки.</summary>
    private const int LinkRoleOffset = 4;

    /// <summary>Код прямого UInt; остальные типы селектора в этих опытах не проверены.</summary>
    private const int UIntType = 0x12;

    /// <summary>Обычные диапазоны SYMB, в отличие от списка битов.</summary>
    private const int RangeMode = 1;

    /// <summary>Две границы по u32 на диапазон.</summary>
    private const int RangeSize = 8;

    /// <summary>Заголовок текстовых подписей по 4 байта, steps-buttons.</summary>
    private const long TextHeader = 0x00040100;

    /// <summary>Заголовок ссылок картинок по 4 байта, steps-buttons.</summary>
    private const long PictureHeader = 0x00040200;

    /// <summary>Прочитать список; неподтверждённый режим или тип тега сохраняет Entries null.</summary>
    /// <returns>Список; ссылки нет — null.</returns>
    /// <exception cref="FwxFormatException">Связанная запись или её ссылки повреждены.</exception>
    public static ButtonCaptionList? Read(FwxDocument doc, TableItem source, long body, string section, ButtonMode mode)
    {
        var links = NamedElement.TryReferences(doc.Binary, source) ?? [];
        var firstLink = source.Offset + body - links.Count * LinkSize;
        for (var index = 0; index < links.Count; index++)
        {
            var link = links[index];
            var position = firstLink + index * LinkSize;
            if (doc.Binary.D2(position + LinkRoleOffset) != ListRole)
            {
                continue;
            }
            var table = doc.FindTable(link.Table)
                ?? throw new FwxFormatException(section, position, "таблица списка кнопки отсутствует в TOC");
            return table.Name == "SYMB"
                ? ReadSymbol(doc, table, ElementLink.Item(doc, table, link.Index, "кнопка"), mode)
                : new ButtonCaptionList(null, null);
        }
        return null;
    }

    /// <summary>Прочитать управляющий тег и записи SYMB с диапазонами прямого UInt (проверено на steps-buttons).</summary>
    /// <returns>Тег и записи; другие режимы или типы — непрочитанные записи.</returns>
    /// <exception cref="FwxFormatException">Диапазоны или ссылки выходят за элемент.</exception>
    private static ButtonCaptionList ReadSymbol(FwxDocument doc, TocEntry table, TableItem item, ButtonMode mode)
    {
        var b = doc.Binary;
        var (end, links) = ReferenceHeader.Read(b, item, table.Name);
        if (links.Count == 0 || doc.FindTable(links[0].Table) is not { Name: "VAR" } tagTable)
        {
            return new ButtonCaptionList(null, null);
        }
        var tag = links[0];
        ElementLink.Item(doc, tagTable, tag.Index, "список кнопки");
        ButtonDataReader.Check(item, end, 4, table.Name);
        var p = item.Offset;
        var count = b.D2(p + end);
        var tagType = doc.Tags.FirstOrDefault(t => t.Index == tag.Index + 1)?.TypeCode;
        if (b.D2(p + end + 2) != RangeMode || tagType != UIntType)
        {
            return new ButtonCaptionList(tag, null);
        }
        var header = end + 4 + count * (long)RangeSize;
        ButtonDataReader.Check(item, header, 4, table.Name);
        var text = mode == ButtonMode.TextList;
        var expected = text ? TextHeader : PictureHeader;
        if (b.D4(p + header) != expected)
        {
            return new ButtonCaptionList(tag, null);
        }
        // У текстового списка перед подписями диапазонов находится ещё одно u32 (в опыте 0).
        var values = header + (text ? 8 : 4);
        ButtonDataReader.Check(item, values, count * 4L, table.Name);
        if (values + count * 4L != item.Length || text && b.D4(p + header + 4) != 0)
        {
            return new ButtonCaptionList(tag, null);
        }
        var entries = new List<ButtonListEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var range = p + end + 4 + i * (long)RangeSize;
            var value = p + values + i * 4L;
            var caption = text
                ? new ButtonCaption(ButtonDataReader.TextIndex(doc, b.D4(value), table.Name, value), null)
                : new ButtonCaption(null, ButtonDataReader.Picture(doc, value, table.Name));
            entries.Add(new ButtonListEntry(b.D4(range), b.D4(range + 4), caption));
        }
        return new ButtonCaptionList(tag, entries);
    }
}
