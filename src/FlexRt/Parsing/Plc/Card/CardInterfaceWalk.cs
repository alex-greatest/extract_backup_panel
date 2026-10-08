using System.Xml.Linq;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>Состояние обхода интерфейса блока карты (<see cref="CardInterfaceBuilder"/>).</summary>
/// <param name="Kids">Вложенные члены по цепочке ID; ключ <c>""</c> — члены разделов FB.</param>
/// <param name="Udts">Общий словарь FB и UDT по имени для всего интерфейса DB.</param>
/// <param name="CommentsByName">Комментарии членов FB и UDT по имени блока.</param>
/// <param name="Section">Где искать при ошибке: файл <c>OMSSTORE</c> блока.</param>
/// <param name="Ancestors">Части, которые читаются сейчас выше по цепочке: повтор — цикл ссылок.</param>
internal sealed record CardInterfaceWalk(
    Dictionary<string, List<PlcDbMember>> Kids,
    Dictionary<string, PlcDbInterface> Udts,
    IReadOnlyDictionary<string, Dictionary<string, string>> CommentsByName,
    string Section,
    HashSet<XElement> Ancestors);
