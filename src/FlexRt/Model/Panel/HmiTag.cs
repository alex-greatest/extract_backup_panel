namespace FlexRt.Model.Panel;

/// <summary>Тег HMI из таблицы VAR.</summary>
/// <param name="Index">Номер записи в таблице VAR, с единицы.</param>
/// <param name="Name">Имя тега.</param>
/// <param name="TypeCode">Код типа как в файле; бит <see cref="ArrayFlag"/> — массив.</param>
/// <param name="Elements">Число элементов: 1 у скаляра, у массива <c>[0..N-1]</c> — N.</param>
/// <param name="Data4">4-байтовое поле хвоста записи. Гипотеза: смещение значения в буфере тегов.</param>
/// <param name="Offset">Смещение записи от начала файла.</param>
/// <param name="LinkTable">
/// Id таблицы, на которую ссылается тег (у PLC-тега — DATALINK_READWR); у внутреннего тега
/// <c>null</c>.
/// </param>
/// <param name="LinkIndex">Номер элемента в таблице <paramref name="LinkTable"/>, с нуля; у внутреннего тега <c>null</c>.</param>
public sealed record HmiTag(int Index, string Name, int TypeCode, int Elements, long Data4, long Offset, int? LinkTable, int? LinkIndex)
{
    /// <summary>Бит массива в коде типа: <c>0x2005</c> — Array of LReal, <c>0x0005</c> — LReal.</summary>
    public const int ArrayFlag = 0x2000;
}
