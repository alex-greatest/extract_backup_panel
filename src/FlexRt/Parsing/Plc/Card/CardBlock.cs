using System.Xml.Linq;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>Блок карты (DB, FB, FC, UDT), прочитанный из своих потоков (<see cref="CardDbParser"/>).</summary>
/// <param name="Rid">RID объекта блока.</param>
/// <param name="Section">Где искать при ошибке: файл <c>OMSSTORE</c> первого потока блока.</param>
/// <param name="Xml">Интерфейс <c>&lt;BlockInterface&gt;</c>; <c>null</c>, если его нет или он не разобран.</param>
/// <param name="Comments">Комментарии членов блока по цепочке ID.</param>
/// <param name="Problem">Почему поток блока не разобран; <c>null</c> — без проблем.</param>
internal sealed record CardBlock(long Rid, string Section, XElement? Xml, Dictionary<string, string> Comments, string? Problem);
