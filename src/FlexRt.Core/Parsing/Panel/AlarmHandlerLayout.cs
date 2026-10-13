using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>
/// Раскладка элемента обработчика алармов, общая для MSG_NUMHANDLER (дискретные) и
/// MSG_ANALOGHANDL (аналоговые): число алармов, два списка и записи классов.
/// </summary>
internal static class AlarmHandlerLayout
{
    /// <summary>
    /// Смещение числа алармов N (u16) в элементе. Поля до него не разобраны (наблюдение: ссылки на
    /// таблицы 0x1a, 0x12, 0x6e, 0x51, 0x5b, 0x5d — одинаковы во всех файлах и в обеих таблицах).
    /// </summary>
    private const int AlarmCountOffset = 0x2c;

    /// <summary>
    /// Начало двух массивов u16[N] сразу за числом алармов. Наблюдение на A603A0097: первый — ID
    /// алармов по возрастанию, второй — номер элемента таблицы алармов в том же порядке.
    /// </summary>
    private const int ListsOffset = 0x2e;

    /// <summary>Размер записи класса.</summary>
    private const int ClassSize = 0x24;

    /// <summary>
    /// Смещение ID класса (u8) в записи. Наблюдение: 1 Errors, 2 Warnings, 0x21 Acknowledgement,
    /// 0x22 No Acknowledgement, свои — 0x40, 0x41 (TIA V21) и 0x23..0x27 (TIA V17); сверено с
    /// полем ID класса в TIA на A603A0097.
    /// </summary>
    private const int ClassIdOffset = 0x20;

    /// <summary>
    /// Один элемент: на <see cref="AlarmCountOffset"/> число алармов N, за ним два массива u16[N],
    /// u16 — число классов и записи классов по <see cref="ClassSize"/> байт до конца элемента.
    /// Запись класса: <c>+0x00</c> u32 строка Display name, <c>+0x04</c>..<c>+0x0c</c> строки
    /// состояний (наблюдение: <c>I</c>, <c>O</c>, <c>A</c>), <c>+0x10</c> четыре цвета фона,
    /// <c>+0x20</c> ID класса, <c>+0x21</c> два байта модели квитирования (наблюдение:
    /// <c>0c 03</c> — с квитированием, <c>04 01</c> — без), <c>+0x23</c> 0. Каждый класс сразу
    /// добавляется в <paramref name="classes"/>.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент короче заголовка или его длина не сходится с числом классов.</exception>
    public static void ReadClasses(FwxBinary b, TableItem item, List<AlarmClass> classes)
    {
        var number = item.Index + 1;
        if (item.Length < ListsOffset)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {number}: длина {item.Length} меньше заголовка 0x{ListsOffset:x}");
        }
        var alarms = b.D2(item.Offset + AlarmCountOffset);
        var countOffset = ListsOffset + alarms * 4L;
        if (countOffset + 2 > item.Length)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {number}: {alarms} алармов не помещаются в {item.Length} байт");
        }
        var count = b.D2(item.Offset + countOffset);
        var classesOffset = countOffset + 2;
        if (classesOffset + count * (long)ClassSize != item.Length)
        {
            throw new FwxFormatException(b.Section, item.Offset, $"запись {number}: длина {item.Length} не сходится с {count} классами");
        }
        for (var i = 0; i < count; i++)
        {
            var p = item.Offset + classesOffset + i * (long)ClassSize;
            classes.Add(new AlarmClass(b.D1(p + ClassIdOffset), b.D4(p)));
        }
    }
}
