using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Parsing.Panel.Alarms;

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

    /// <summary>Смещение номера строки Display name (u32) в записи класса.</summary>
    private const int DisplayNameOffset = 0x00;

    /// <summary>Смещение номера строки текста состояния Incoming (u32) в записи класса.</summary>
    private const int IncomingTextOffset = 0x04;

    /// <summary>Смещение номера строки текста состояния Outgoing (u32) в записи класса.</summary>
    private const int OutgoingTextOffset = 0x08;

    /// <summary>Смещение номера строки текста состояния Acknowledged (u32) в записи класса.</summary>
    private const int AcknowledgedTextOffset = 0x0c;

    /// <summary>Смещение первого из четырёх цветов фона (u32) в записи класса.</summary>
    private const int ColorsOffset = 0x10;

    /// <summary>Число цветов фона в записи класса.</summary>
    private const int ColorCount = 4;

    /// <summary>Смещение двух байт State machine (u16) в записи класса.</summary>
    private const int StateMachineOffset = 0x21;

    /// <summary>
    /// Один элемент: на <see cref="AlarmCountOffset"/> число алармов N, за ним два массива u16[N],
    /// u16 — число классов и записи классов по <see cref="ClassSize"/> байт до конца элемента.
    /// Запись класса (<see cref="ReadClass"/>) сразу добавляется в <paramref name="classes"/>.
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
            classes.Add(ReadClass(b, item.Offset + classesOffset + i * (long)ClassSize));
        }
    }

    /// <summary>
    /// Запись класса с позиции <paramref name="p"/> (сверено с TIA на шаговых копиях A603A0097 и
    /// Project1, V21): <c>+0x00</c> u32 строка Display name, <c>+0x04</c>, <c>+0x08</c>,
    /// <c>+0x0c</c> строки состояний Incoming, Outgoing, Acknowledged, <c>+0x10</c> четыре цвета
    /// фона по u32 (байты R, G, B и признак), <c>+0x20</c> ID класса, <c>+0x21</c> u16 State
    /// machine (<c>0c 03</c> — с квитированием, <c>04 01</c> — без), <c>+0x23</c> 0.
    /// </summary>
    /// <returns>Класс аларма.</returns>
    private static AlarmClass ReadClass(FwxBinary b, long p)
    {
        var colors = Enumerable.Range(0, ColorCount).Select(c => b.D4(p + ColorsOffset + c * 4L)).ToList();
        return new AlarmClass(b.D1(p + ClassIdOffset), b.D4(p + DisplayNameOffset), b.D4(p + IncomingTextOffset), b.D4(p + OutgoingTextOffset), b.D4(p + AcknowledgedTextOffset), colors, b.D2(p + StateMachineOffset));
    }
}
