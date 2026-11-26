using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens.Objects;

/// <summary>Список дочерних объектов VIEW_DARK и VIEW_MOVE за заголовком ссылок.</summary>
internal static class ObjectWrapperReader
{
    /// <summary>Обёртка объекта с условиями; за ссылками одна пара дочернего объекта.</summary>
    public const string DarkTable = "VIEW_DARK";

    /// <summary>Контейнер; за ссылками u16 (наблюдение: 0), u16 N и N пар дочерних объектов.</summary>
    public const string MoveTable = "VIEW_MOVE";

    /// <summary>Смещение u16 числа пар VIEW_MOVE от конца ссылок.</summary>
    private const int MoveCountOffset = 0x02;

    /// <summary>Смещение первой пары VIEW_MOVE от конца ссылок.</summary>
    private const int MovePairsOffset = 0x04;

    /// <summary>Предельная глубина обхода обёрток.</summary>
    public const int MaxDepth = 8;

    /// <summary>Прочитать пары дочерних объектов от конца ссылок обёртки.</summary>
    /// <returns>Ссылки на объекты в порядке пар.</returns>
    /// <exception cref="FwxFormatException">Число объектов или пары не помещаются в элемент.</exception>
    public static List<VarReference> Read(FwxBinary binary, TocEntry table, TableItem item, long end)
    {
        var isMove = table.Name == MoveTable;
        var offset = isMove ? end + MovePairsOffset : end;
        if (isMove && offset > item.Length)
        {
            throw new FwxFormatException(table.Name, item.Offset,
                $"запись {item.Index + 1}: число объектов не помещается в {item.Length} байт");
        }
        var count = isMove ? binary.D2(item.Offset + end + MoveCountOffset) : 1;
        return offset + count * (long)ObjectPairs.Size <= item.Length
            ? ObjectPairs.Read(binary, item.Offset + offset, count)
            : throw new FwxFormatException(table.Name, item.Offset,
                $"запись {item.Index + 1}: {count} объектов не помещаются в {item.Length} байт");
    }
}
