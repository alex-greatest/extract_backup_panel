using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Список пар элемента ADAPTVALUECHANG за ссылками: u16 (наблюдение: всегда 0x12, не разобрано), u16 K, u32 вид списка
/// (наблюдение: 2 — номера экранов, <see cref="AdaptValueChangParser"/>; 3 — цвета Limits I/O field,
/// <see cref="IoFieldLimitReader"/>), K пар своего размера.
/// </summary>
internal static class ValueChangeList
{
    /// <summary>Имя таблицы.</summary>
    public const string Table = "ADAPTVALUECHANG";

    /// <summary>Смещение u16 числа пар K от конца ссылок.</summary>
    private const int CountOffset = 2;

    /// <summary>Смещение u32 вида списка от конца ссылок.</summary>
    private const int KindOffset = 4;

    /// <summary>Начало пар от конца ссылок.</summary>
    private const int PairsOffset = 8;

    /// <summary>Вид списка за ссылками элемента, не проверяя пары.</summary>
    /// <param name="b">Байты файла.</param>
    /// <param name="item">Элемент ADAPTVALUECHANG.</param>
    /// <param name="end">Конец ссылок от начала элемента.</param>
    /// <returns>Вид списка; заголовок списка не помещается в элемент — <c>null</c>.</returns>
    public static long? TryKind(FwxBinary b, TableItem item, long end) =>
        end + PairsOffset <= item.Length ? b.D4(item.Offset + end + KindOffset) : null;

    /// <summary>
    /// Прочитать заголовок списка за ссылками элемента.
    /// </summary>
    /// <param name="b">Байты файла.</param>
    /// <param name="item">Элемент ADAPTVALUECHANG.</param>
    /// <param name="end">Конец ссылок от начала элемента (<see cref="ReferenceHeader"/>).</param>
    /// <param name="pairSize">Размер пары в байтах.</param>
    /// <param name="section">Имя таблицы — для сообщения.</param>
    /// <returns>Смещение первой пары в файле, число пар K и вид списка.</returns>
    /// <exception cref="FwxFormatException">Число пар или сами пары не помещаются в элемент.</exception>
    public static (long Pairs, int Count, long Kind) Read(FwxBinary b, TableItem item, long end, int pairSize, string section)
    {
        var p = item.Offset;
        if (end + PairsOffset > item.Length)
        {
            throw new FwxFormatException(section, p, $"запись {item.Index + 1}: число пар не помещается в {item.Length} байт");
        }
        var count = b.D2(p + end + CountOffset);
        if (end + PairsOffset + count * (long)pairSize > item.Length)
        {
            throw new FwxFormatException(section, p, $"запись {item.Index + 1}: {count} пар не помещаются в {item.Length} байт");
        }
        return (p + end + PairsOffset, count, b.D4(p + end + KindOffset));
    }
}
