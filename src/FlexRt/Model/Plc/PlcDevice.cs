namespace FlexRt.Model.Plc;

/// <summary>ПЛК из проекта TIA (PEData.plf) и IP-адреса его интерфейсов.</summary>
/// <param name="Id">ID объекта ПЛК.</param>
/// <param name="Name">Имя ПЛК (<c>PLC_1</c>); <c>null</c>, если не найдено.</param>
public sealed record PlcDevice(long Id, string? Name)
{
    /// <summary>
    /// IP-адреса интерфейсов: старший байт — первая часть адреса. Флаг — интерфейс
    /// подключён к подсети.
    /// </summary>
    public List<(uint Ip, bool InSubnet)> Addresses { get; } = [];
}
