namespace FlexRt.Model.Panel;

/// <summary>Сетевые параметры панели: элемент таблицы DEVICE_OMSP.</summary>
/// <param name="Ip">IP-адрес панели: старший байт — первая часть адреса (<c>0xc0a80103</c> = 192.168.1.3).</param>
/// <param name="Mask">Маска подсети в том же виде (<c>0xffffff00</c> = 255.255.255.0).</param>
public sealed record HmiDevice(uint Ip, uint Mask);
