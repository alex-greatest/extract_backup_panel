namespace FlexRt.Model.Panel;

/// <summary>Соединение панели с ПЛК: элемент таблицы CONNECTION_OMSP.</summary>
/// <param name="Index">Номер соединения, с нуля; на него ссылается <see cref="PlcLink.Connection"/>.</param>
/// <param name="Name">Имя соединения, как в TIA (<c>HMI_Connection_1</c>).</param>
/// <param name="Ip">IP-адрес ПЛК: старший байт — первая часть адреса (<c>0xc0a80001</c> = 192.168.0.1).</param>
public sealed record HmiConnection(int Index, string Name, uint Ip);
