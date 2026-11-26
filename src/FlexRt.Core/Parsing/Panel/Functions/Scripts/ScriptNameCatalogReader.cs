using System.Text;
using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Functions.Scripts;

/// <summary>Имена VB-скриптов из FUNC_COMMONSCRI в порядке номеров FUNC_SCRIPT.</summary>
internal static class ScriptNameCatalogReader
{
    /// <summary>Имя таблицы каталога VB-скриптов.</summary>
    private const string TableName = "FUNC_COMMONSCRI";

    /// <summary>Слово перед числом имён в проверенных распакованных каталогах.</summary>
    private const int CatalogMarker = 2;

    /// <summary>Размер записи имени с пустой строкой: u32 длины, u16 нуля и u16 служебного слова.</summary>
    private const int MinimumNameRecordSize = 0x08;

    /// <summary>Размер порции имени при чтении через границы блоков.</summary>
    private const int NameBufferSize = 0x100;

    /// <summary>Кодировка имён. Повреждённый UTF-16 не заменяется символами подстановки.</summary>
    private static readonly UnicodeEncoding NameEncoding = new(false, false, true);

    /// <summary>Прочитать имена каталога целиком и сохранить их в документе после проверки блоков.</summary>
    /// <exception cref="FwxFormatException">Неизвестный заголовок или повреждены данные каталога.</exception>
    public static void Parse(FwxDocument doc)
    {
        if (doc.FindTable(TableName) is not { Entries: > 0 } table)
        {
            return;
        }
        if (table.Entries != 1)
        {
            throw new FwxFormatException(table.Name, table.Offset, $"элементов каталога VB: {table.Entries}, ожидался один");
        }
        doc.ScriptNames = Read(doc, table);
    }

    /// <summary>Прочитать пронумерованные имена из единственной записи, пропуская исходный текст.</summary>
    /// <returns>Имена в порядке каталога.</returns>
    /// <exception cref="FwxFormatException">Структура записи, блоков или каталога не сходится.</exception>
    private static string[] Read(FwxDocument doc, TocEntry table)
    {
        var item = ElementLink.Item(doc, table, 0);
        var reader = new ScriptBlockReader(doc.Binary, table, item);
        SkipSource(reader);
        if (reader.ReadWord() != CatalogMarker)
        {
            throw new FwxFormatException(table.Name, reader.BlockOffset, "неизвестный заголовок списка имён VB");
        }
        var count = reader.ReadWord();
        if (count * MinimumNameRecordSize > reader.Remaining)
        {
            throw new FwxFormatException(table.Name, reader.BlockOffset, "имена не помещаются в распакованные данные");
        }
        var names = new string[count];
        for (var index = 0; index < count; index++)
        {
            names[index] = ReadName(reader);
        }
        reader.Finish();
        return names;
    }

    /// <summary>Пропустить u32 длины и текст до завершающего нуля UTF-16.</summary>
    /// <exception cref="FwxFormatException">Длина текста некорректна или нет завершающего нуля.</exception>
    private static void SkipSource(ScriptBlockReader reader)
    {
        var size = reader.ReadDoubleWord();
        if (size < sizeof(ushort) || size % sizeof(ushort) != 0 || size > reader.Remaining)
        {
            throw new FwxFormatException(TableName, reader.BlockOffset, "неверная длина исходного текста VB");
        }
        reader.Skip(size - sizeof(ushort));
        if (reader.ReadWord() != 0)
        {
            throw new FwxFormatException(TableName, reader.BlockOffset, "у исходного текста VB нет завершающего нуля");
        }
    }

    /// <summary>Прочитать длину, имя UTF-16 с нулём и следующее служебное слово.</summary>
    /// <returns>Имя скрипта; пустая строка сохраняет номер записи в каталоге.</returns>
    /// <exception cref="FwxFormatException">Имя не помещается или повреждено.</exception>
    private static string ReadName(ScriptBlockReader reader)
    {
        var size = reader.ReadDoubleWord();
        if (size < sizeof(ushort) || size % sizeof(ushort) != 0 || size > Array.MaxLength
            || size > reader.Remaining - sizeof(ushort))
        {
            throw new FwxFormatException(TableName, reader.BlockOffset, "неверная длина имени VB");
        }
        var name = ReadNameText(reader, (int)size - sizeof(ushort));
        if (reader.ReadWord() != 0)
        {
            throw new FwxFormatException(TableName, reader.BlockOffset, "у имени VB нет завершающего нуля");
        }
        reader.Skip(sizeof(ushort));
        return name;
    }

    /// <summary>Прочитать имя порциями, выделяя память только под проверенные символы.</summary>
    /// <returns>Имя скрипта.</returns>
    /// <exception cref="FwxFormatException">Повреждён блок или недопустим символ имени.</exception>
    private static string ReadNameText(ScriptBlockReader reader, int size)
    {
        Span<byte> bytes = stackalloc byte[NameBufferSize];
        Span<char> chars = stackalloc char[NameBufferSize / sizeof(char) + 1];
        var decoder = NameEncoding.GetDecoder();
        var name = new StringBuilder();
        try
        {
            while (size > 0)
            {
                var count = Math.Min(size, bytes.Length);
                reader.Read(bytes[..count]);
                size -= count;
                var length = decoder.GetChars(bytes[..count], chars, flush: size == 0);
                var text = chars[..length];
                if (text.Contains('\0'))
                {
                    throw new FwxFormatException(TableName, reader.BlockOffset, "имя VB содержит нулевой символ");
                }
                name.Append(text);
            }
        }
        catch (DecoderFallbackException)
        {
            throw new FwxFormatException(TableName, reader.BlockOffset, "имя VB содержит повреждённый текст UTF-16");
        }
        return name.ToString();
    }
}
