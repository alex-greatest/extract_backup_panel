using System.Text;
using System.Text.RegularExpressions;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>Конфигурация оборудования на карте ПЛК: модель CPU.</summary>
internal static partial class CardHardware
{
    /// <summary>
    /// Название CPU в объекте оборудования: байт длины, затем <c>CPU …</c> (наблюдение:
    /// <c>0e "CPU 1515F-2 PN"</c> в <c>OMSSTORE\00000002\0\0000005A</c>, карта S7-1500, TIA V19).
    /// </summary>
    [GeneratedRegex(@"CPU [\x20-\x7e]{1,60}")]
    private static partial Regex CpuName();

    /// <summary>Заказной номер Siemens: <c>6ES7 515-2FN03-0AB0</c> (пробел после <c>6ES7</c> бывает не всегда).</summary>
    [GeneratedRegex(@"6ES7 ?\d{3}-[0-9A-Z]{5}-[0-9A-Z]{4}")]
    private static partial Regex OrderNumber();

    /// <summary>Сколько байт после названия CPU искать его заказной номер (наблюдение: номер через 0x3a байт).</summary>
    private const int OrderNumberWindow = 0x100;

    /// <summary>
    /// Модель CPU: первое название <c>CPU …</c>, перед которым стоит байт его длины, и первый
    /// заказной номер не дальше <see cref="OrderNumberWindow"/> байт за ним, в файлах
    /// <c>OMSSTORE</c> в порядке путей. Номер модулей ввода-вывода в тех же файлах стоит без
    /// названия <c>CPU</c>, поэтому не попадает.
    /// </summary>
    /// <returns><c>CPU 1515F-2 PN (6ES7 515-2FN03-0AB0)</c> или <c>null</c>, если не найдено.</returns>
    /// <exception cref="IOException">Файл карты не удалось прочитать.</exception>
    public static string? CpuModel(string storeDirectory)
    {
        foreach (var path in Directory.EnumerateFiles(storeDirectory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (FindCpu(Encoding.Latin1.GetString(File.ReadAllBytes(path))) is { } model)
            {
                return model;
            }
        }
        return null;
    }

    /// <summary>Название CPU с байтом длины перед ним и заказной номер за ним в тексте файла (Latin-1: байт — символ).</summary>
    /// <returns>Модель или <c>null</c>.</returns>
    private static string? FindCpu(string text)
    {
        foreach (Match name in CpuName().Matches(text))
        {
            var length = name.Index > 0 ? text[name.Index - 1] : '\0';
            if (length < 5 || length > name.Length)
            {
                continue;
            }
            var cpu = name.Value[..length];
            var tail = text.Substring(name.Index + length, Math.Min(OrderNumberWindow, text.Length - name.Index - length));
            var order = OrderNumber().Match(tail);
            if (order.Success)
            {
                return $"{cpu} ({order.Value})";
            }
        }
        return null;
    }
}
