using System.Xml.Linq;
using System.Xml;
using FlexRt.Binary;
using FlexRt.Model.Plc.Plf;

namespace FlexRt.Parsing.Plc.Plf.DataBlocks;

/// <summary>XML интерфейса (<c>&lt;Root&gt;</c> корня, <c>&lt;Member&gt;</c> вложенного члена) внутри объекта PEData.plf.</summary>
internal static class PlfInterfaceXml
{
    /// <summary>
    /// Достать и разобрать XML объекта. XML лежит открыто (BOM и <c>&lt;Root</c> или
    /// <c>&lt;Member</c>) либо цепочкой чанков <c>[varint длина][zlib-поток]</c>: каждый
    /// распаковывается в 4096 байт, последний добит нулями. Текст обрезается по закрывающему тегу.
    /// Объекты с другим XML (<c>&lt;Values&gt;</c> — начальные значения) и без XML не считаются
    /// ошибкой: у них нет членов.
    /// </summary>
    /// <returns>Корневой элемент (<c>Root</c> или <c>Member</c>) или <c>null</c>, если в объекте нет такого XML.</returns>
    /// <exception cref="FwxFormatException">XML найден, но не закрыт или не разбирается.</exception>
    public static XElement? Read(FwxBinary b, PlfObject obj)
    {
        var xml = PlfXmlBytes.Find(b.Span(obj.Offset, obj.Length));
        if (xml is null)
        {
            return null;
        }
        var text = PlfXmlBytes.CutToClosingTag(xml);
        if (text is null)
        {
            throw new FwxFormatException(PlfFormat.Section, obj.Offset, $"XML объекта 0x{obj.Class:x} {obj.Id} не закрыт");
        }
        try
        {
            return XDocument.Parse(text).Root!;
        }
        catch (XmlException e)
        {
            throw new FwxFormatException(PlfFormat.Section, obj.Offset, $"XML объекта 0x{obj.Class:x} {obj.Id}: {e.Message}");
        }
    }
}
