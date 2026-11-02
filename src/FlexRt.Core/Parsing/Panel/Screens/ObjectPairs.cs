using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Список объектов: пары u16 номер элемента, u16 Id таблицы — порядок обратный ссылкам (у ссылки сначала Id таблицы).
/// Так записаны объекты записи SCREEN и вложенные объекты VIEW_MOVE и VIEW_DARK.
/// </summary>
internal static class ObjectPairs
{
    /// <summary>Размер пары: u16 номер элемента, u16 Id таблицы.</summary>
    public const int Size = 4;

    /// <summary>Прочитать пары подряд; что они помещаются в элемент, проверяет вызывающий.</summary>
    /// <param name="b">Байты файла.</param>
    /// <param name="offset">Смещение первой пары от начала файла.</param>
    /// <param name="count">Число пар.</param>
    /// <returns>Пары как ссылки: Id таблицы и номер элемента.</returns>
    public static List<VarReference> Read(FwxBinary b, long offset, int count) =>
    [
        .. Enumerable.Range(0, count)
            .Select(i => offset + i * Size)
            .Select(o => new VarReference(b.D2(o + 2), b.D2(o)))
    ];
}
