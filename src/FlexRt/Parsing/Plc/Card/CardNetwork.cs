using System.Buffers.Binary;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>Сетевые параметры CPU на карте ПЛК: IP-адреса его интерфейсов.</summary>
internal static class CardNetwork
{
    /// <summary>Имя объекта сетевых параметров интерфейса (атрибут имени объекта оборудования).</summary>
    private static readonly byte[] NetworkParameters = [.. "Network Parameters"u8];

    /// <summary>
    /// Метка перед IP интерфейса в объекте «Network Parameters» (наблюдение на карте S7-1500, TIA V19,
    /// <c>OMSSTORE\00000002\0\0000003F</c>): за ней IP, маска и шлюз по 4 байта, первая часть
    /// адреса первым байтом; дальше — PROFINET-имя интерфейса (<c>tm50.x1</c>). IP X1 192.168.1.1
    /// совпал с адресом ПЛК в соединении панели.
    /// </summary>
    private static readonly byte[] AddressMark = [0xa0, 0x01, 0x00, 0x14, 0x30, 0x00, 0x00, 0x10, 0x01, 0x00, 0x00, 0x00];

    /// <summary>Сколько байт после имени «Network Parameters» искать метку адреса (наблюдение: метка через 0x20 байт).</summary>
    private const int MarkWindow = 0x100;

    /// <summary>Сколько байт перед именем искать начало объекта (наблюдение: начало за 0x0c–0x0f байт до имени).</summary>
    private const int ObjectWindow = 0x40;

    /// <summary>
    /// Добавить IP-адреса интерфейсов CPU из одного файла <c>OMSSTORE</c>: объекты «Network
    /// Parameters», у которых RID объекта меньше 0x10000 (наблюдение: объекты CPU — <c>0x4d</c> X1,
    /// <c>0x4e</c> X2; у устройств сети — <c>0x08ddxxxx</c>, их адреса не берутся), адрес за
    /// <see cref="AddressMark"/> (старший байт — первая часть). Адреса, которые уже есть в списке
    /// (в том числе из прежних файлов), и нулевой адрес не добавляются.
    /// </summary>
    public static void AddCpuAddresses(byte[] data, List<uint> addresses)
    {
        var from = 0;
        while (from < data.Length)
        {
            var name = data.AsSpan(from).IndexOf(NetworkParameters);
            if (name < 0)
            {
                return;
            }
            var at = from + name;
            from = at + NetworkParameters.Length;
            if (ObjectRidBefore(data, at) is not < 0x10000 || AddressAfter(data, from) is not { } address || address == 0 || addresses.Contains(address))
            {
                continue;
            }
            addresses.Add(address);
        }
    }

    /// <summary>Адрес за <see cref="AddressMark"/> не дальше <see cref="MarkWindow"/> байт от позиции.</summary>
    /// <returns>Адрес или <c>null</c>, если метки нет.</returns>
    private static uint? AddressAfter(byte[] data, int start)
    {
        var mark = data.AsSpan(start, Math.Min(MarkWindow, data.Length - start)).IndexOf(AddressMark);
        var ip = start + mark + AddressMark.Length;
        return mark >= 0 && ip + 4 <= data.Length ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(ip)) : null;
    }

    /// <summary>
    /// RID ближайшего начала объекта (<see cref="CardObjectStart.IsAt"/>) перед позицией: не дальше
    /// <see cref="ObjectWindow"/> байт, RID целиком до позиции.
    /// </summary>
    /// <returns>RID или <c>null</c>, если начала объекта нет.</returns>
    private static long? ObjectRidBefore(byte[] data, int index)
    {
        for (var p = index - 1; p >= Math.Max(0, index - ObjectWindow); p--)
        {
            if (p + CardObjectStart.Size < index && CardObjectStart.IsAt(data, p))
            {
                return CardObjectStart.Rid(data, p);
            }
        }
        return null;
    }
}
