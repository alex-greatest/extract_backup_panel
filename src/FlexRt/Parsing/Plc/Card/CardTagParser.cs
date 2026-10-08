using System.Xml.Linq;
using FlexRt.Binary;
using FlexRt.Model.Plc.Card;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>
/// Теги ПЛК карты: таблицы <c>&lt;IdentContainer&gt;</c> (по одной на область, в своём файле
/// <c>OMSSTORE</c>) и комментарии <c>&lt;CommentDictionary&gt;</c> того же файла.
/// </summary>
internal static class CardTagParser
{
    /// <summary>Область по атрибуту <c>Range</c>. На карте встречались Input, Output, Memory; таймеры и счётчики не встречались.</summary>
    private static readonly Dictionary<string, (PlcArea Area, string Letter)> Areas = new()
    {
        ["Input"] = (PlcArea.Input, "I"),
        ["Output"] = (PlcArea.Output, "Q"),
        ["Memory"] = (PlcArea.Memory, "M")
    };

    /// <summary>Буква размера адреса по атрибуту <c>Width</c>; у <c>Bit</c> адрес с номером бита.</summary>
    private static readonly Dictionary<string, string> Widths = new()
    {
        ["Byte"] = "B",
        ["Word"] = "W",
        ["DWord"] = "D"
    };

    /// <summary>
    /// Прочитать все теги карты. Тег, который не удалось разобрать (нет имени, LID, незнакомая
    /// область или ширина адреса), попадает в <see cref="PlcProject.Problems"/> и считается в
    /// <see cref="PlcProject.UnparsedTags"/>; остальные читаются.
    /// </summary>
    /// <exception cref="FwxFormatException">Поток таблицы или комментариев не разбирается как XML.</exception>
    public static void ReadAll(List<CardStream> streams, long plcId, PlcProject project)
    {
        foreach (var file in streams.GroupBy(s => s.File))
        {
            var comments = new Dictionary<string, string>();
            foreach (var stream in file.Where(s => s.Kind == CardStreamKind.TagComments))
            {
                CardXml.AddComments(stream, "RefID", comments);
            }
            foreach (var table in file.Where(s => s.Kind == CardStreamKind.TagInterface))
            {
                foreach (var ident in CardXml.Parse(table).Elements("Ident"))
                {
                    ReadTag(ident, plcId, comments, project);
                }
            }
        }
    }

    /// <summary>
    /// Один тег: имя, <c>LID</c> (он же ID символа, на который ссылается панель), тип
    /// (<c>&lt;SimpleType&gt;</c> или UDT в кавычках, как в TIA), адрес из
    /// <c>&lt;SimpleAccess Range Width ByteNumber BitNumber&gt;</c> и комментарий по <c>LID</c>.
    /// Не разобран — сообщение в <see cref="PlcProject.Problems"/>, счётчик <see cref="PlcProject.UnparsedTags"/>.
    /// </summary>
    private static void ReadTag(XElement ident, long plcId, Dictionary<string, string> comments, PlcProject project)
    {
        var name = (string?)ident.Attribute("Name");
        var lidText = (string?)ident.Attribute("LID") ?? "";
        var access = ident.Element("Access")?.Element("SimpleAccess");
        var (area, address) = access is null ? (null, null) : FormatAddress(access);
        if (name is null || !long.TryParse(lidText, out var lid) || address is null)
        {
            project.UnparsedTags++;
            project.Problems.Add($"тег ПЛК {name ?? "?"} (LID {lidText}): нет имени, LID или знакомого адреса");
            return;
        }
        project.Tags.Add(new PlcTag(name, plcId, DataType(ident), address, area, lid, comments.GetValueOrDefault(lidText, "")));
    }

    /// <summary>Тип тега: <c>&lt;SimpleType&gt;</c> как есть, <c>&lt;UserDataType&gt;</c> — в кавычках.</summary>
    /// <returns>Тип или <c>null</c>, если его нет.</returns>
    private static string? DataType(XElement ident)
    {
        var simple = (string?)ident.Element("SimpleType");
        var udt = (string?)ident.Element("UserDataType");
        return simple ?? (udt is null ? null : $"\"{udt}\"");
    }

    /// <summary>
    /// Адрес как в TIA: <c>%I13100.0</c> у бита (нет <c>BitNumber</c> — бит 0), <c>%IW128</c>,
    /// <c>%MB1</c>, <c>%ID…</c> у байта, слова, двойного слова.
    /// </summary>
    /// <returns>Область и адрес; адрес <c>null</c>, если область или ширина незнакомы.</returns>
    private static (PlcArea? Area, string? Address) FormatAddress(XElement access)
    {
        var range = (string?)access.Attribute("Range") ?? "";
        var width = (string?)access.Attribute("Width") ?? "";
        if (!Areas.TryGetValue(range, out var area) || !long.TryParse((string?)access.Attribute("ByteNumber"), out var number))
        {
            return (null, null);
        }
        if (width != "Bit")
        {
            return Widths.TryGetValue(width, out var size) ? (area.Area, $"%{area.Letter}{size}{number}") : (null, null);
        }
        var bit = long.TryParse((string?)access.Attribute("BitNumber"), out var b) ? b : 0;
        return (area.Area, $"%{area.Letter}{number}.{bit}");
    }
}
