using FlexRt.Binary;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc.Devices;

/// <summary>
/// ПЛК из PEData.plf: объект ПЛК (класс 0x0010101c) — имя, объекты сетевых интерфейсов
/// (класс 0x0010101d) — IP-адреса. Интерфейс ссылается на свой ПЛК связью 0x00012003.
/// </summary>
internal static class PlfDeviceParser
{
    /// <summary>Класс объекта «сетевой интерфейс».</summary>
    private const long InterfaceClass = 0x0010101d;

    /// <summary>Тип связи «интерфейс → подсеть»: есть только у подключённого интерфейса.</summary>
    private const long SubnetRelation = 0x00102046;

    /// <summary>Наблюдение: первое слово блока адреса интерфейса; длина блока = оно + префикс имени интерфейса.</summary>
    private const long AddressMark = 0x21;

    /// <summary>Префикс строки имени интерфейса от начала блока адреса: 8 байт заголовка, три u64, 9 байт флагов.</summary>
    private const int AddressNamePos = 0x29;

    /// <summary>Окончание строки типа CPU (<c>S71500.CPU</c>, <c>S71200.CPU</c>), за которой идут комментарий и имя.</summary>
    private const string CpuTypeSuffix = ".CPU";

    /// <summary>В каком начальном отрезке объекта ПЛК ищется строка типа CPU.</summary>
    private const int NameSearchLength = 0x4000;

    /// <summary>
    /// Собрать ПЛК проекта и IP-адреса их интерфейсов. Интерфейс без IP (0.0.0.0) или без
    /// связи с ПЛК пропускается.
    /// </summary>
    /// <returns>ПЛК в порядке ID объекта.</returns>
    public static List<PlcDevice> Read(PlfFile file)
    {
        var devices = file.OfClassById(PlfFormat.PlcClass)
            .Select(o => new PlcDevice(o.Id, FindName(file.Binary, o)))
            .ToList();

        foreach (var iface in file.OfClass(InterfaceClass))
        {
            var relations = PlfRelations.Scan(file, iface);
            var device = devices.FirstOrDefault(d => d.Id == PlfRelations.PlcId(relations));
            var ip = FindIp(file.Binary, iface);
            if (device is null || ip is null)
            {
                continue;
            }
            device.Addresses.Add((ip.Value, relations.Any(r => r.Type == SubnetRelation)));
        }
        return devices;
    }

    /// <summary>
    /// Имя ПЛК: строка сразу после строки типа CPU (<c>…​.CPU</c>) и комментария — мультиязычного
    /// текста или пустой строки (байт <c>01</c>). Установлено на всех 32 живых объектах ПЛК семи
    /// проектов (S7-1500, S7-1200, разные авторы и порядок строк перед типом). От имени автора и
    /// длины строк не зависит.
    /// </summary>
    /// <returns>Имя ПЛК или <c>null</c>, если строка типа CPU не найдена.</returns>
    private static string? FindName(FwxBinary b, PlfObject obj)
    {
        var end = obj.Offset + obj.Length;
        var limit = Math.Min(end, obj.Offset + NameSearchLength);
        for (var p = obj.Offset + PlfFormat.MinSlot; p < limit; p++)
        {
            if (TryReadNameAfterCpu(b, p, end) is { } name)
            {
                return name;
            }
        }
        return null;
    }

    /// <summary>
    /// С позиции <paramref name="p"/>: строка типа CPU (буквы, цифры, точка, кончается на
    /// <c>.CPU</c>), затем мультиязычный текст или пустая строка, затем строка имени.
    /// </summary>
    /// <returns>Имя или <c>null</c>, если цепочка не сходится.</returns>
    private static string? TryReadNameAfterCpu(FwxBinary b, long p, long end)
    {
        var prefix = b.D1(p);
        if (prefix < CpuTypeSuffix.Length + 2 || prefix > 0x40 || p + prefix > end)
        {
            return null;
        }
        return PlfText.Attempt(() => ReadNameAfterCpuType(b, p, end));
    }

    /// <summary>Строка типа CPU, комментарий и имя с позиции <paramref name="p"/>.</summary>
    /// <returns>Имя или <c>null</c>, если строка не типа CPU или имя пустое.</returns>
    /// <exception cref="FwxFormatException">Цепочка не сходится: вызывающий ищет дальше.</exception>
    private static string? ReadNameAfterCpuType(FwxBinary b, long p, long end)
    {
        var (type, afterType) = PlfText.ReadString(b, p, end);
        if (!type.EndsWith(CpuTypeSuffix, StringComparison.Ordinal) || !type.All(c => char.IsAsciiLetterOrDigit(c) || c == '.'))
        {
            return null;
        }
        var afterComment = b.D1(afterType) == 1 ? afterType + 1 : PlfText.ReadMultilingual(b, afterType, end).Next;
        var (name, _) = PlfText.ReadString(b, afterComment, end);
        return name.Length > 0 ? name : null;
    }

    /// <summary>
    /// IP интерфейса: единственный блок <c>[u32 0x21][u32 L]</c>, за ним u64 IP, u64 маска,
    /// u64 маршрутизатор (little endian, старшие 32 бита IP и маски нулевые), 9 байт флагов и
    /// строка имени интерфейса; L = 0x21 + префикс этой строки (установлено на семи проектах).
    /// </summary>
    /// <returns>IP (старший байт — первая часть адреса) или <c>null</c>, если блока нет, их несколько или IP 0.</returns>
    private static uint? FindIp(FwxBinary b, PlfObject obj)
    {
        var end = obj.Offset + obj.Length;
        var blocks = new List<long>();
        for (var p = obj.Offset + PlfFormat.MinSlot; p + AddressNamePos + 1 <= end; p++)
        {
            var length = b.D4(p + 4);
            if (b.D4(p) == AddressMark && length == AddressMark + b.D1(p + AddressNamePos) && p + 8 + length <= end)
            {
                blocks.Add(p + 8);
            }
        }
        if (blocks.Count != 1 || b.D4(blocks[0] + 4) != 0 || b.D4(blocks[0] + 0x0c) != 0)
        {
            return null;
        }
        var ip = (uint)b.D4(blocks[0]);
        return ip == 0 ? null : ip;
    }
}
