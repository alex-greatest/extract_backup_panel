namespace FlexRt.Export;

/// <summary>IP-адрес в точечной записи — для листа «Сводка» и для лога запуска.</summary>
internal static class IpAddressText
{
    /// <summary>Адрес в точечной записи: старший байт — первая часть.</summary>
    /// <returns><c>192.168.1.1</c> для <c>0xc0a80101</c>.</returns>
    public static string Format(uint ip) => $"{ip >> 24}.{(ip >> 16) & 0xff}.{(ip >> 8) & 0xff}.{ip & 0xff}";
}
