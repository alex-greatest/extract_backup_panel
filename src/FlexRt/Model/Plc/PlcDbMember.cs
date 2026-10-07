namespace FlexRt.Model.Plc;

/// <summary>Член интерфейса DB, FB или UDT: элемент <c>&lt;Member&gt;</c> в XML интерфейса.</summary>
/// <param name="Id">Атрибут <c>ID</c>: номер члена внутри корня интерфейса; из цепочки ID строится <c>ParentId</c> вложенных членов.</param>
/// <param name="Name">Имя члена.</param>
/// <param name="Type">Тип как в TIA (пустой у скаляров простых типов, см. <paramref name="Rid"/>): <c>Real</c>, <c>Array[1..10] of Real</c>, <c>String[30]</c>, <c>"UDT_X"</c> (с кавычками).</param>
/// <param name="Rid">Атрибут <c>RID</c> (<c>0x02000008</c>): у скаляра простого типа вместо <c>Type</c> хранится код типа; -1, если атрибута нет.</param>
/// <param name="Lid">Атрибут <c>LID</c>: номер члена внутри родителя, его хранит цепочка пути в pdata; <c>-1</c>, если атрибута нет (раздел FB).</param>
public sealed record PlcDbMember(string Id, string Name, string Type, long Rid, long Lid);
