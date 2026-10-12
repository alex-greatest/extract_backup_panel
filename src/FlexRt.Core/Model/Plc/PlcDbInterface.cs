namespace FlexRt.Model.Plc;

/// <summary>
/// Корень интерфейса (объект 0x0022160c): члены верхнего уровня, вложенные члены по
/// <c>ParentId</c> и связанные UDT.
/// </summary>
/// <param name="RootId">ID объекта корня.</param>
/// <param name="Top">Члены верхнего уровня (дети <c>&lt;Root&gt;</c>).</param>
/// <param name="Kids">Вложенные члены по <c>ParentId</c> (цепочка ID через <c>:</c>); ключ <c>""</c> — у членов без <c>ParentId</c>.</param>
/// <param name="Udts">Корни UDT по имени из строки <c>BIVE:имя/guid</c>; у самих UDT этот словарь пуст.</param>
/// <param name="Comments">Непустые комментарии членов по цепочке ID (<c>52:51:51</c>) от этого корня; пусто, если у владельца корня нет объекта комментариев.</param>
public sealed record PlcDbInterface(
    long RootId,
    IReadOnlyList<PlcDbMember> Top,
    IReadOnlyDictionary<string, List<PlcDbMember>> Kids,
    IReadOnlyDictionary<string, PlcDbInterface> Udts,
    IReadOnlyDictionary<string, string> Comments);
