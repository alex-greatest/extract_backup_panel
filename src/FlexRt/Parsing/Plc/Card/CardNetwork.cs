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

    /// <summary>Байт начала объекта: за ним u32 RID big endian, varint и байт <see cref="ObjectBodyTag"/>.</summary>
    private const byte ObjectMark = 0xa6;

    /// <summary>Байт начала тела объекта сразу за varint после RID.</summary>
    private const byte ObjectBodyTag = 0xa3;

    /// <summary>Наибольшая длина varint за RID в байтах (u32).</summary>
    private const int MaxVarintLength = 5;

    /// <summary>
    /// IP-адреса интерфейсов CPU: объекты «Network Parameters», у которых RID объекта меньше
    /// 0x10000 (наблюдение: объекты CPU — <c>0x4d</c> X1, <c>0x4e</c> X2; у устройств сети —
    /// <c>0x08ddxxxx</c>, их адреса не берутся), адрес за <see cref="AddressMark"/>. Повторы и
    /// нулевой адрес не попадают.
    /// </summary>
    /// <returns>Адреса по порядку файлов (старший байт — первая часть); пусто, если не найдено.</returns>
    /// <exception cref="IOException">Файл карты не удалось прочитать.</exception>
    public static List<uint> CpuAddresses(string storeDirectory)
    {
        var addresses = new List<uint>();
        foreach (var path in Directory.EnumerateFiles(storeDirectory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            AddAddresses(File.ReadAllBytes(path), addresses);
        }
        return addresses;
    }

    /// <summary>Добавить адреса объектов «Network Parameters» CPU одного файла.</summary>
    private static void AddAddresses(byte[] data, List<uint> addresses)
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
    /// RID ближайшего начала объекта перед позицией (не дальше <see cref="ObjectWindow"/> байт):
    /// <see cref="ObjectMark"/>, u32 RID, varint, <see cref="ObjectBodyTag"/>.
    /// </summary>
    /// <returns>RID или <c>null</c>, если начала объекта нет.</returns>
    private static long? ObjectRidBefore(byte[] data, int index)
    {
        for (var p = index - 1; p >= Math.Max(0, index - ObjectWindow); p--)
        {
            if (data[p] == ObjectMark && p + 5 < index && IsBodyAfterVarint(data, p + 5))
            {
                return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(p + 1));
            }
        }
        return null;
    }

    /// <summary>За varint на позиции стоит <see cref="ObjectBodyTag"/>.</summary>
    /// <returns><c>true</c>, если так.</returns>
    private static bool IsBodyAfterVarint(byte[] data, int start)
    {
        var end = Math.Min(data.Length, start + MaxVarintLength);
        var at = start;
        while (at < end && (data[at] & 0x80) != 0)
        {
            at++;
        }
        return at + 1 < data.Length && data[at + 1] == ObjectBodyTag;
    }
}
