using System.Xml.Linq;
using System.Xml;
using FlexRt.Binary;
using FlexRt.Model.Plc.Card;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>XML распакованного потока карты ПЛК.</summary>
internal static class CardXml
{
    /// <summary>Разобрать текст потока как XML; BOM в начале текста отбрасывается.</summary>
    /// <returns>Корневой элемент.</returns>
    /// <exception cref="FwxFormatException">Текст не разбирается как XML: файл и смещение потока в сообщении.</exception>
    public static XElement Parse(CardStream stream)
    {
        try
        {
            return XDocument.Parse(stream.Text.TrimStart('\uFEFF')).Root!;
        }
        catch (XmlException e)
        {
            throw new FwxFormatException(stream.File, stream.Offset, $"XML не разбирается: {e.Message}");
        }
    }

    /// <summary>
    /// Комментарии из XML с элементами <c>&lt;Comment&gt;</c> (ключ — атрибут
    /// <paramref name="keyAttribute"/>): текст первого непустого <c>&lt;DictEntry&gt;</c>, как у
    /// комментариев PEData.plf. Комментарии без текста не попадают; при повторном ключе остаётся первый.
    /// Пустой поток — комментариев нет (наблюдение: у блоков без комментариев поток распаковывается в 0 байт).
    /// </summary>
    /// <exception cref="FwxFormatException">Поток не разбирается как XML.</exception>
    public static void AddComments(CardStream stream, string keyAttribute, Dictionary<string, string> comments)
    {
        if (string.IsNullOrWhiteSpace(stream.Text))
        {
            return;
        }
        foreach (var comment in Parse(stream).Descendants("Comment"))
        {
            var key = (string?)comment.Attribute(keyAttribute);
            var text = comment.Elements("DictEntry").Select(e => e.Value).FirstOrDefault(v => v.Length > 0);
            if (key is not null && text is not null)
            {
                comments.TryAdd(key, text);
            }
        }
    }
}
