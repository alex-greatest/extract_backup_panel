using FlexRt.Model.Plc.Card;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>
/// Словари zlib, которыми сжаты потоки карты ПЛК. Поток zlib с флагом FDICT хранит после
/// заголовка ID словаря — его Adler-32; без словаря поток не распаковать. Словари взяты из
/// проекта S7CommPlusDriver (Thomas Wiens, https://github.com/thomas-v2/S7CommPlusDriver,
/// файл <c>src/S7CommPlusDriver/Core/BlobDecompressor.cs</c>, лицензия LGPL-3.0) и лежат
/// ресурсами сборки (<c>Dictionaries/*.bin</c>). Используются только четыре словаря, нужные
/// для тегов и интерфейсов блоков; установлено на карте S7-1500 с данными TIA V19.
/// </summary>
internal static class CardDictionaries
{
    /// <summary>Известные словари: ID → вид потока и имя ресурса.</summary>
    private static readonly Dictionary<uint, (CardStreamKind Kind, string Resource)> Known = new()
    {
        [0xce9b821b] = (CardStreamKind.TagInterface, "intf-desc-tag-90000001"),
        [0xe2729ea1] = (CardStreamKind.TagComments, "tag-line-comm-90000001"),
        [0x66052b13] = (CardStreamKind.BlockInterface, "debug-info-intf-desc-98000001"),
        [0x3c55436a] = (CardStreamKind.BlockComments, "line-comm-98000001")
    };

    /// <summary>Байты словарей по ID, загружаются из ресурсов при первом обращении к классу.</summary>
    private static readonly Dictionary<uint, byte[]> Bytes = Known.ToDictionary(kv => kv.Key, kv => Load(kv.Value.Resource));

    /// <summary>Словарь по ID из потока zlib.</summary>
    /// <returns><c>true</c>, если словарь известен; тогда вид потока и байты словаря.</returns>
    public static bool TryGet(uint id, out CardStreamKind kind, out byte[] dictionary)
    {
        if (!Known.TryGetValue(id, out var known))
        {
            kind = default;
            dictionary = [];
            return false;
        }
        kind = known.Kind;
        dictionary = Bytes[id];
        return true;
    }

    /// <summary>Прочитать ресурс словаря <c>FlexRt.Card.&lt;имя&gt;.bin</c>.</summary>
    /// <returns>Байты словаря.</returns>
    /// <exception cref="InvalidOperationException">Ресурса нет в сборке: ошибка сборки, а не входного файла.</exception>
    private static byte[] Load(string name)
    {
        using var stream = typeof(CardDictionaries).Assembly.GetManifestResourceStream($"FlexRt.Card.{name}.bin")
            ?? throw new InvalidOperationException($"в сборке нет словаря {name}");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
