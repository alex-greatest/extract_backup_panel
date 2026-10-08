using System.Text;
using System.Text.RegularExpressions;
using FlexRt.Model.Plc.Card;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>Конфигурация оборудования на карте ПЛК: имя и модель CPU.</summary>
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

    /// <summary>Сколько байт перед названием CPU искать атрибут имени объекта (наблюдение: имя за 0xd1 байт до названия на обеих картах).</summary>
    private const int NameWindow = 0x100;

    /// <summary>Сколько байт после названия CPU искать его заказной номер (наблюдение: номер через 0x3a байт).</summary>
    private const int OrderNumberWindow = 0x100;

    /// <summary>
    /// Атрибут имени объекта оборудования: <c>a3 81 69 00 15</c>, байт длины, строка (наблюдение: в
    /// начале объекта CPU <c>05 " TM50"</c>, объектов станции и интерфейсов — <c>04 "TM50"</c>; из
    /// этого имени TIA строит PROFINET-имена <c>tm50.x1</c>).
    /// </summary>
    private const string NameAttribute = "£\u0081i\0\u0015";

    /// <summary>
    /// CPU в одном файле <c>OMSSTORE</c> (текст Latin-1: байт — символ): первое название <c>CPU …</c>,
    /// перед которым стоит байт его длины, и первый заказной номер не дальше
    /// <see cref="OrderNumberWindow"/> байт за ним (номера модулей ввода-вывода стоят без названия
    /// <c>CPU</c> и не попадают); имя — из ближайшего перед названием атрибута имени объекта, без
    /// пробелов по краям. Карта — первый CPU в файлах по порядку путей.
    /// </summary>
    /// <returns>Имя и модель CPU или <c>null</c>, если в файле CPU нет.</returns>
    public static CardCpu? FindCpu(byte[] data)
    {
        var text = Encoding.Latin1.GetString(data);
        foreach (Match name in CpuName().Matches(text))
        {
            var length = name.Index > 0 ? text[name.Index - 1] : '\0';
            if (length < 5 || length > name.Length)
            {
                continue;
            }
            var tail = text.Substring(name.Index + length, Math.Min(OrderNumberWindow, text.Length - name.Index - length));
            var order = OrderNumber().Match(tail);
            if (order.Success)
            {
                return new CardCpu(ObjectName(text, name.Index), $"{name.Value[..length]} ({order.Value})");
            }
        }
        return null;
    }

    /// <summary>
    /// Имя из ближайшего перед позицией атрибута <see cref="NameAttribute"/> не дальше
    /// <see cref="NameWindow"/> байт (имя самого объекта CPU, не соседнего), без пробелов по краям.
    /// Имя не из печатных символов ASCII (кириллица, иероглифы) не берётся — тогда ПЛК называется
    /// по папке карты.
    /// </summary>
    /// <returns>Имя или <c>null</c>, если атрибута нет или имя пустое.</returns>
    private static string? ObjectName(string text, int before)
    {
        var at = text.LastIndexOf(NameAttribute, before, StringComparison.Ordinal);
        var start = at + NameAttribute.Length + 1;
        if (at < 0 || start > before || before - at > NameWindow)
        {
            return null;
        }
        var length = text[start - 1];
        var name = start + length <= before ? text.Substring(start, length).Trim() : "";
        return name.Length > 0 && name.All(c => c is >= ' ' and <= '~') ? name : null;
    }
}
