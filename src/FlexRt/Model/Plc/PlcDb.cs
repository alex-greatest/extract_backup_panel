namespace FlexRt.Model.Plc;

/// <summary>Блок данных ПЛК (объект 0x00221002) и деревья его членов.</summary>
/// <param name="Name">Имя DB; <c>null</c>, если не найдено.</param>
/// <param name="Number">Номер DB.</param>
/// <param name="PlcId">ID объекта ПЛК, которому принадлежит DB; 0, если связи с ПЛК нет.</param>
public sealed record PlcDb(string? Name, int Number, long PlcId)
{
    /// <summary>
    /// Корни интерфейса по предпочтению: свой снимок для HMI (0x22260e), свой текущий (0x22260a),
    /// затем корни FB для instance-DB.
    /// </summary>
    public List<PlcDbInterface> Interfaces { get; } = [];

    /// <summary>Сообщение, почему интерфейс прочитан не полностью; <c>null</c> — без проблем.</summary>
    public string? Problem { get; set; }
}
