using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Разбор SCREEN: экраны панели — имя, размер и список объектов.</summary>
public static class ScreenParser
{
    /// <summary>Первое слово записи экрана (как у записи VAR).</summary>
    private const int ScreenMarker = 3;

    /// <summary>Первое слово служебной записи pop-up экрана (имя у неё пустое).</summary>
    private const int ServiceMarker = 1;

    /// <summary>Размер ссылки: u16 Id таблицы, u16 номер элемента и два слова.</summary>
    private const int ReferenceSize = 8;

    /// <summary>Наибольшее число ссылок, которое принимается без ошибки (наблюдение: 0…2).</summary>
    private const long MaxReferences = 0x20;

    /// <summary>
    /// Смещение u16 ширины от конца ссылок; перед ней 8 байт (наблюдение: у экрана
    /// <c>40 00 00 40 00 00 00 00</c>, у служебной записи <c>00 00 00 40 00 00 00 00</c>), за ней u16 высота.
    /// </summary>
    private const int SizeOffset = 0x08;

    /// <summary>Смещение u16 числа объектов от конца ссылок: за размером 6 байт (наблюдение: всегда нули).</summary>
    private const int ObjectCountOffset = 0x12;

    /// <summary>Размер объекта: u16 номер элемента, u16 Id таблицы.</summary>
    private const int ObjectSize = 4;

    /// <summary>
    /// Прочитать таблицу SCREEN и добавить записи в <see cref="FwxDocument.Screens"/> в порядке каталога.
    /// Нет таблицы в TOC — ничего не делает.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или не сходится с раскладкой.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("SCREEN");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            doc.Screens.Add(ReadRecord(doc.Binary, item));
        }
    }

    /// <summary>
    /// Запись (наблюдение на Project1, A603A0097 и CCB-A13): u16 вид (3 — экран, 1 — служебная запись pop-up),
    /// u16 длина имени в символах, имя UTF-16LE, нули до кратности 4, u32 R — число ссылок, R ссылок по 8 байт
    /// (у экрана — на SOFTKEY и, если есть, FUNC_LIST или ADAPTDEMULTIPLE), 8 байт, u16 ширина, u16 высота, 6 байт, u16 N — число
    /// объектов, N пар (u16 номер элемента, u16 Id таблицы). Ссылки и байты после объектов не читаются.
    /// </summary>
    /// <returns>Запись экрана.</returns>
    /// <exception cref="FwxFormatException">
    /// Элемент короче 4 байт, первое слово не 3 и не 1, R больше 0x20 или имя, ссылки, размер и объекты не помещаются в элемент.
    /// </exception>
    private static Screen ReadRecord(FwxBinary b, TableItem item)
    {
        var p = item.Offset;
        var number = item.Index + 1;
        if (item.Length < 4)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: длина {item.Length} меньше 4");
        }
        var marker = b.D2(p);
        if (marker != ScreenMarker && marker != ServiceMarker)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: первое слово {marker}, ожидалось 3 или 1");
        }
        var chars = b.D2(p + 2);
        var countOffset = (4 + chars * 2L + 3) / 4 * 4;
        if (countOffset + 4 > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: имя из {chars} символов не помещается в {item.Length} байт");
        }
        var references = b.D4(p + countOffset);
        var body = countOffset + 4 + references * ReferenceSize;
        if (references > MaxReferences || body + ObjectCountOffset + 2 > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: {references} ссылок не помещаются в {item.Length} байт");
        }
        var objects = b.D2(p + body + ObjectCountOffset);
        var first = body + ObjectCountOffset + 2;
        if (first + objects * (long)ObjectSize > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {number}: {objects} объектов не помещаются в {item.Length} байт");
        }
        var list = Enumerable.Range(0, objects)
            .Select(i => p + first + i * ObjectSize)
            .Select(o => new VarReference(b.D2(o + 2), b.D2(o)))
            .ToList();
        var name = b.GetNameLen(p + 4, chars);
        var width = b.D2(p + body + SizeOffset);
        var height = b.D2(p + body + SizeOffset + 2);
        return new Screen(item.Index, marker == ServiceMarker, name, width, height, list);
    }
}
