using FlexRt.Binary;

namespace FlexRt.Parsing.Plc.Plf.Tags;

/// <summary>Секция имени объекта «тег ПЛК»: имя тега и его комментарий.</summary>
internal static class PlfTagNames
{
    /// <summary>
    /// Старший байт отметки времени .NET (UTC), которая лежит прямо перед полем секции имени.
    /// Наблюдение: у всех тегов семи проектов.
    /// </summary>
    private const int TimestampHighByte = 0x48;

    /// <summary>Поле секции имени, когда имя идёт первым; иначе оно равно 0x1e + длина комментария.</summary>
    private const long NameFirst = 0x1e;

    /// <summary>Сколько байт от слота начала секции имени просматривается в поисках поля.</summary>
    private const int SearchWindow = 0x40;

    /// <summary>
    /// Секция имени: в первых 0x40 байтах от слота ищется поле после отметки времени (байт
    /// 0x48 перед ним). Поле 0x1e — имя, затем, возможно, комментарий; поле 0x1e + X —
    /// комментарий длиной X, затем имя. Секция должна кончиться на <c>+0x50 − 4</c> или
    /// <c>+0x58 − 4</c> (допускаются нули до конца).
    /// </summary>
    /// <returns>Имя и комментарий (пустой, если его нет).</returns>
    /// <exception cref="FwxFormatException">Секция имени не найдена.</exception>
    public static (string Name, string Comment) Read(FwxBinary b, long start, long afterNames, long relations)
    {
        var limit = Math.Min(start + SearchWindow, afterNames - 4);
        for (var p = start; p < limit; p++)
        {
            if (b.D1(p - 1) != TimestampHighByte)
            {
                continue;
            }
            var at = p;
            if (PlfText.Attempt(() => TryReadAt(b, at, afterNames, relations)) is { } found)
            {
                return found;
            }
        }
        throw new FwxFormatException(PlfFormat.Section, start, "секция имени тега не найдена");
    }

    /// <summary>Попытка прочитать секцию имени с поля на позиции <paramref name="p"/>.</summary>
    /// <returns>Имя и комментарий или <c>null</c>, если раскладка не сошлась.</returns>
    /// <exception cref="FwxFormatException">Поле не начало секции имени: вызывающий ищет дальше.</exception>
    private static (string Name, string Comment)? TryReadAt(FwxBinary b, long p, long afterNames, long relations)
    {
        var field = b.D4(p);
        if (field == NameFirst)
        {
            var (name, afterName) = PlfText.ReadString(b, p + 4, afterNames);
            if (EndsSection(b, afterName, afterNames, relations))
            {
                return (name, "");
            }
            var (comment, afterComment) = PlfText.ReadMultilingual(b, afterName, afterNames);
            return EndsSection(b, afterComment, afterNames, relations) ? (name, comment) : null;
        }
        var (length, _) = PlfText.ReadVarint(b, p + 4);
        if (field != NameFirst + length)
        {
            return null;
        }
        var (text, afterText) = PlfText.ReadMultilingual(b, p + 4, afterNames);
        var (tagName, afterTagName) = PlfText.ReadString(b, afterText, afterNames);
        return EndsSection(b, afterTagName, afterNames, relations) ? (tagName, text) : null;
    }

    /// <summary>Секция кончается на <paramref name="pos"/>: до одной из границ только нули.</summary>
    /// <returns><c>true</c>, если позиция — конец секции.</returns>
    private static bool EndsSection(FwxBinary b, long pos, long afterNames, long relations)
    {
        long[] bounds = [afterNames - 4, relations - 4];
        return bounds.Any(bound => pos <= bound && b.Span(pos, bound - pos).IndexOfAnyExcept((byte)0) < 0);
    }
}
