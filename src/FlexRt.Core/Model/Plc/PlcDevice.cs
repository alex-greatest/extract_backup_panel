namespace FlexRt.Model.Plc;

/// <summary>ПЛК из проекта TIA (PEData.plf) или с карты ПЛК и IP-адреса его интерфейсов (с карты — из объекта «Network Parameters»).</summary>
/// <param name="Id">ID объекта ПЛК.</param>
/// <param name="Name">Имя ПЛК (<c>PLC_1</c>); <c>null</c>, если не найдено.</param>
public sealed record PlcDevice(long Id, string? Name)
{
    /// <summary>
    /// IP-адреса интерфейсов: старший байт — первая часть адреса. Флаг — интерфейс
    /// подключён к подсети.
    /// </summary>
    public List<(uint Ip, bool InSubnet)> Addresses { get; } = [];

    /// <summary>Модель CPU с заказным номером (<c>CPU 1515F-2 PN (6ES7 515-2FN03-0AB0)</c>); <c>null</c>, если не найдена — сейчас читается только с карты ПЛК.</summary>
    public string? Model { get; set; }
}
