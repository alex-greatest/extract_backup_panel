using FlexRt.Binary;
using FlexRt.Model;

namespace FlexRt.Parsing;

/// <summary>Разбор STRINGSTORE: строки всех языков.</summary>
public static class StringStoreParser
{
    /// <summary>
    /// Прочитать таблицу STRINGSTORE и добавить строки в <see cref="FwxDocument.Strings"/>
    /// в порядке каталога: сначала все строки первого LCID, затем второго и т.д.
    /// Нет таблицы в TOC или она пуста — ничего не делает. Строки добавляются по мере чтения:
    /// если разбор оборвётся, уже прочитанные строки остаются в документе.
    /// Одинаковое смещение (маска <c>0x7fffffff</c>) — одна и та же строка.
    /// </summary>
    /// <exception cref="FwxFormatException">Каталог выходит за пределы файла, строка — за пределы таблицы, или до конца таблицы нет нулевого символа.</exception>
    public static void Parse(FwxDocument doc)
    {
        var store = doc.FindTable("STRINGSTORE");
        if (store is null || store.Entries == 0)
        {
            return;
        }

        var b = doc.Binary;
        b.Section = "STRINGSTORE";

        // метаинформация обо всех строках
        var start = store.Offset + 0x40;
        // смещение заголовка языков от start: 0x1c + каталог смещений строк
        var langHeaderOffset = b.D4(start + 0xc);
        // начало самих строк
        var stringsStart = start + b.D4(start + 0x14);
        // сколько строк в каждом словаре
        var stringsCount = b.D4(start + 0x18);
        // начало каталога смещений
        var tocStart = start + 0x1c;
        var codes = ReadLanguageCodes(b, start + langHeaderOffset);

        var total = stringsCount * codes.Length;
        b.Check(tocStart, total * 4);
        var tableEnd = store.Offset + store.Size;

        var cache = new Dictionary<long, string>();
        for (var n = 0L; n < total; n++)
        {
            var offsetPos = tocStart + n * 4;
            var relative = Relative(b, offsetPos);
            if (!cache.TryGetValue(relative, out var str))
            {
                str = ReadString(b, offsetPos, relative, stringsStart, tableEnd, n + 1 < total);
                cache[relative] = str;
            }
            doc.Strings.Add(new LangString((int)(n % stringsCount) + 1, codes[n / stringsCount], str, relative));
        }
    }

    /// <summary>
    /// Заголовок языков: сначала количество языков, затем столько же кодов LCID по 16 бит.
    /// Лежит сразу за каталогом смещений.
    /// </summary>
    /// <returns>Коды LCID в порядке хранения; их число — число языков.</returns>
    /// <exception cref="FwxFormatException">Заголовок выходит за пределы файла.</exception>
    private static int[] ReadLanguageCodes(FwxBinary b, long langHeaderPos)
    {
        var languages = b.D2(langHeaderPos);
        var codes = new int[languages];
        for (var i = 0; i < languages; i++)
        {
            codes[i] = b.D2(langHeaderPos + (i + 1) * 2);
        }
        return codes;
    }

    /// <summary>
    /// Одна строка языка. Длина — до следующего смещения; если она не положительна (ссылка
    /// на более раннюю строку или последняя строка), ищется нулевой символ. Строка читается
    /// без завершающего нуля; если каталог не по порядку и в длину попала соседняя строка,
    /// хвост обрезается по первому нулю. Нулевой символ ищется только внутри таблицы:
    /// байты соседних таблиц в строку не попадают.
    /// </summary>
    /// <returns>Текст строки.</returns>
    /// <exception cref="FwxFormatException">Строка начинается или кончается за пределами таблицы, либо до конца таблицы нет нулевого символа.</exception>
    private static string ReadString(FwxBinary b, long offsetPos, long relative, long stringsStart, long tableEnd, bool hasNext)
    {
        var absolute = stringsStart + relative;
        // 2 байта - минимум: один символ UTF-16 (завершающий нуль)
        if (absolute + 2 > tableEnd)
        {
            throw new FwxFormatException("STRINGSTORE", offsetPos, $"смещение строки 0x{relative:x} указывает за пределы таблицы");
        }
        var byteLength = hasNext ? Relative(b, offsetPos + 4) - relative : -1;
        if (byteLength <= 0)
        {
            byteLength = NulTerminatedLength(b, absolute, tableEnd);
        }
        if (absolute + byteLength > tableEnd)
        {
            throw new FwxFormatException("STRINGSTORE", offsetPos, $"строка по смещению 0x{relative:x} выходит за пределы таблицы");
        }
        var str = b.GetNameLen(absolute, (int)(byteLength / 2) - 1);
        var nul = str.IndexOf('\0');
        return nul >= 0 ? str[..nul] : str;
    }

    /// <summary>
    /// Длина строки, когда её нельзя вычислить по следующему смещению (ссылка на более
    /// раннюю строку или последняя строка): до первого нулевого символа.
    /// Поиск идёт только внутри таблицы, байты соседних таблиц в строку не попадают.
    /// </summary>
    /// <returns>Длина в байтах вместе с нулевым символом.</returns>
    /// <exception cref="FwxFormatException">До конца таблицы нет нулевого символа.</exception>
    private static long NulTerminatedLength(FwxBinary b, long absolute, long tableEnd)
    {
        var byteLength = 0L;
        while (absolute + byteLength + 2 <= tableEnd && b.D2(absolute + byteLength) != 0)
        {
            byteLength += 2;
        }
        if (absolute + byteLength + 2 > tableEnd)
        {
            throw new FwxFormatException("STRINGSTORE", absolute, "нет нулевого символа до конца таблицы");
        }
        return byteLength + 2;
    }

    /// <summary>Смещение строки относительно начала строк; старший бит — какой-то флаг.</summary>
    /// <returns>Смещение без старшего бита.</returns>
    /// <exception cref="FwxFormatException">Позиция в каталоге выходит за пределы файла.</exception>
    private static long Relative(FwxBinary b, long offsetPos) => b.D4(offsetPos) & 0x7fffffff;
}
