namespace FlexRt.Model.Panel;

/// <summary>Биты одного тега для дискретных алармов: элемент таблицы AREAPTR.</summary>
/// <param name="Index">Номер элемента в таблице, с нуля; на него ссылается запись VAR тега.</param>
/// <param name="Role">Чем служит тег.</param>
/// <param name="Bits">Пары «аларм — бит тега» в порядке файла.</param>
public sealed record AlarmBits(int Index, AlarmBitRole Role, IReadOnlyList<AlarmBit> Bits);
