using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Разбор SCREEN: экраны панели — имя, размер, ссылки, список объектов и цвет фона.</summary>
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
    /// Смещение цвета фона от конца записи: три байта R, G, B, за ними байт (наблюдение: всегда 0, не читается). Хвост
    /// записи — 20 байт <c>FFFFFFFF | xxxxxxxx | RR GG BB 00 | 00100000 | 00000000</c> (наблюдение на 15 файлах V17 и V21);
    /// цвет сверен с TIA (шаг Project1 и 49 экранов A603A0097).
    /// </summary>
    private const int BackgroundFromEnd = 12;

    /// <summary>
    /// Прочитать таблицу SCREEN и добавить записи в <see cref="FwxDocument.Screens"/> в порядке каталога.
    /// Нет таблицы в TOC — ничего не делает. У записей, где после объектов меньше 12 байт, цвета фона нет
    /// (<see cref="Screen.BackgroundRgb"/> — <c>null</c>): запись остаётся, после таблицы — одно сводное предупреждение,
    /// и тогда, когда разбор оборвала следующая запись (оно идёт перед её предупреждением).
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или не сходится с раскладкой.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("SCREEN");
        if (table is null)
        {
            return;
        }

        var noColor = new List<TableItem>();
        try
        {
            foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
            {
                var screen = ReadRecord(doc.Binary, item);
                doc.Screens.Add(screen);
                if (screen.BackgroundRgb is null)
                {
                    noColor.Add(item);
                }
            }
        }
        finally
        {
            // сбой следующей записи не должен прятать, почему у записей до него цвет «?»
            WarnNoColor(doc, noColor);
        }
    }

    /// <summary>Сводное предупреждение о записях без цвета фона: их число, первая запись и её смещение; таких нет — ничего.</summary>
    private static void WarnNoColor(FwxDocument doc, List<TableItem> noColor)
    {
        if (noColor.Count > 0)
        {
            doc.Warnings.Add($"SCREEN: цвет фона не прочитан у записей: {noColor.Count} (после объектов меньше {BackgroundFromEnd} байт), первая — запись {noColor[0].Index + 1} @ 0x{noColor[0].Offset:X}");
        }
    }

    /// <summary>
    /// Запись (наблюдение на Project1, A603A0097 и CCB-A13): u16 вид (3 — экран, 1 — служебная запись pop-up),
    /// u16 длина имени в символах, имя UTF-16LE, нули до кратности 4, u32 R — число ссылок, R ссылок по 8 байт
    /// (u16 Id таблицы, u16 номер элемента, два слова не читаются; у экрана — на SOFTKEY и, если есть, FUNC_LIST,
    /// ADAPTDEMULTIPLE или LIMIT), 8 байт, u16 ширина, u16 высота, 6 байт, u16 N — число
    /// объектов, N пар (u16 номер элемента, u16 Id таблицы); на длине записи − 12 — цвет фона R, G, B
    /// (<see cref="Background"/>). Остальные байты после объектов не читаются.
    /// </summary>
    /// <returns>Запись экрана; после объектов меньше 12 байт — без цвета фона.</returns>
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
        var links = Enumerable.Range(0, (int)references)
            .Select(i => p + countOffset + 4 + i * ReferenceSize)
            .Select(o => new VarReference(b.D2(o), b.D2(o + 2)))
            .ToList();
        var name = b.GetNameLen(p + 4, chars);
        var width = b.D2(p + body + SizeOffset);
        var height = b.D2(p + body + SizeOffset + 2);
        var background = Background(b, item, first + objects * (long)ObjectSize);
        return new Screen(item.Index, marker == ServiceMarker, name, width, height, list, background, links);
    }

    /// <summary>
    /// Цвет фона записи: байты R, G, B на длине записи − 12, если после объектов (с <paramref name="objectsEnd"/> от
    /// начала записи) не меньше 12 байт.
    /// </summary>
    /// <returns><c>R &lt;&lt; 16 | G &lt;&lt; 8 | B</c>; хвост короче — <c>null</c>.</returns>
    private static int? Background(FwxBinary b, TableItem item, long objectsEnd)
    {
        if (item.Length - objectsEnd < BackgroundFromEnd)
        {
            return null;
        }
        var color = item.Offset + item.Length - BackgroundFromEnd;
        return b.D1(color) << 16 | b.D1(color + 1) << 8 | b.D1(color + 2);
    }
}
