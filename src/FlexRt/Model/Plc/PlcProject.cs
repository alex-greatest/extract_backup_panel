namespace FlexRt.Model.Plc;

/// <summary>Данные ПЛК из проекта TIA (PEData.plf) или с карты ПЛК: ПЛК, их теги, блоки данных и объекты, которые не удалось разобрать.</summary>
public sealed class PlcProject
{
    /// <summary>ПЛК проекта.</summary>
    public List<PlcDevice> Devices { get; } = [];
    /// <summary>Теги ПЛК всех ПЛК проекта.</summary>
    public List<PlcTag> Tags { get; } = [];
    /// <summary>Блоки данных всех ПЛК проекта с деревьями членов.</summary>
    public List<PlcDb> Dbs { get; } = [];
    /// <summary>
    /// Пользовательские типы (UDT) по ПЛК и имени, со вложенными UDT: нужны для членов тегов I/Q/M
    /// пользовательского типа. ID ПЛК 0 — владелец типа не найден.
    /// </summary>
    public Dictionary<(long PlcId, string Name), PlcDbInterface> Udts { get; } = [];
    /// <summary>Объекты (теги ПЛК, DB), раскладку которых не удалось разобрать: сообщение с ID объекта.</summary>
    public List<string> Problems { get; } = [];
    /// <summary>Сколько тегов ПЛК не удалось разобрать (часть <see cref="Problems"/>).</summary>
    public int UnparsedTags { get; set; }
    /// <summary>Сколько DB не удалось прочитать из-за отсутствия номера: такой DB может быть искомым.</summary>
    public int UnnumberedDbs { get; set; }
}
