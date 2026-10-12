using System.Buffers.Binary;
using System.Text;
using FlexRt.Binary;
using FlexRt.Model.Plc.Card;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib;

namespace FlexRt.Parsing.Plc.Card;

/// <summary>
/// Файлы <c>SIMATIC.S7S\OMSSTORE</c> карты ПЛК: поиск в них потоков zlib со словарём, распаковка
/// и привязка потока к объекту, в котором он лежит.
/// </summary>
internal static class CardStore
{
    /// <summary>Первый байт заголовка zlib: deflate, окно 32 КБ.</summary>
    private const byte ZlibHeader = 0x78;

    /// <summary>Флаг FDICT во втором байте заголовка zlib: за заголовком — ID словаря.</summary>
    private const int PresetDictionaryFlag = 0x20;

    /// <summary>Заголовок zlib со словарём: два байта заголовка и u32 ID словаря.</summary>
    private const int DictionaryHeaderSize = 6;

    /// <summary>Размер буфера распаковки.</summary>
    private const int BufferSize = 0x4000;

    /// <summary>
    /// Пройти файл и добавить его распакованные потоки известных видов (<see cref="CardDictionaries"/>)
    /// в порядке смещений. Поток zlib со словарём распаковывается и пропускается целиком, начало
    /// объекта (<see cref="IsObjectStart"/>) задаёт объект для следующих потоков. Метки внутри сжатых
    /// потоков не видны: поток пропускается до проверки меток. Потоки без словаря и со словарём,
    /// которого нет в <see cref="CardDictionaries"/>, пропускаются: в таблицы тегов и интерфейсы
    /// блоков они не входят.
    /// </summary>
    /// <exception cref="FwxFormatException">Поток с известным словарём не распаковывается.</exception>
    public static void ReadFile(byte[] data, string name, List<CardStream> streams)
    {
        long rid = 0;
        var pos = 0;
        while (pos < data.Length)
        {
            if (TryInflate(data, pos, name, out var kind, out var text, out var used))
            {
                streams.Add(new CardStream(name, pos, rid, kind, text));
                pos += used;
                continue;
            }
            if (IsObjectStart(data, pos))
            {
                rid = CardObjectStart.Rid(data, pos);
                pos += CardObjectStart.Size;
                continue;
            }
            pos++;
        }
    }

    /// <summary>
    /// Начало объекта блока (<see cref="CardObjectStart.IsAt"/>) с RID со старшим байтом 0x89 или
    /// 0x8a. Без проверки тела за varint метку дают случайные байты, в том числе внутри потоков с
    /// незнакомым словарём между DB и его интерфейсом.
    /// </summary>
    /// <returns><c>true</c>, если на позиции начало объекта.</returns>
    private static bool IsObjectStart(byte[] data, int pos) => CardObjectStart.IsAt(data, pos) && data[pos + 1] is 0x89 or 0x8a;

    /// <summary>
    /// Распаковать поток zlib на позиции, если это поток со словарём из <see cref="CardDictionaries"/>:
    /// заголовок <c>78 xx</c> с верной контрольной суммой, флаг FDICT, известный ID.
    /// </summary>
    /// <returns><c>true</c> и текст потока (UTF-8), вид и число байт потока; <c>false</c>, если здесь нет такого потока.</returns>
    /// <exception cref="FwxFormatException">Заголовок и ID словаря верны, но поток не распаковывается или обрывается.</exception>
    private static bool TryInflate(byte[] data, int pos, string name, out CardStreamKind kind, out string text, out int used)
    {
        text = "";
        used = 0;
        kind = default;
        if (data[pos] != ZlibHeader || pos + DictionaryHeaderSize > data.Length)
        {
            return false;
        }
        var flags = data[pos + 1];
        var id = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos + 2));
        // FCHECK zlib: два байта заголовка как u16 big endian кратны 31
        if ((ZlibHeader * 256 + flags) % 31 != 0 || (flags & PresetDictionaryFlag) == 0 || !CardDictionaries.TryGet(id, out kind, out var dictionary))
        {
            return false;
        }
        try
        {
            (text, used) = Inflate(data, pos, dictionary, name);
            return true;
        }
        catch (SharpZipBaseException e)
        {
            throw new FwxFormatException(name, pos, $"поток zlib не распаковывается: {e.Message}");
        }
    }

    /// <summary>Распаковать поток zlib с заголовком, подставив словарь, когда распаковщик его попросит.</summary>
    /// <returns>Текст потока и сколько байт он занял.</returns>
    /// <exception cref="SharpZipBaseException">Данные потока испорчены.</exception>
    /// <exception cref="FwxFormatException">Поток обрывается на конце файла или распаковщик встал.</exception>
    private static (string Text, int Used) Inflate(byte[] data, int pos, byte[] dictionary, string name)
    {
        var inflater = new Inflater();
        inflater.SetInput(data, pos, data.Length - pos);
        using var output = new MemoryStream();
        var buffer = new byte[BufferSize];
        while (!inflater.IsFinished)
        {
            var count = inflater.Inflate(buffer);
            if (count > 0)
            {
                output.Write(buffer, 0, count);
            }
            else if (inflater.IsNeedingDictionary)
            {
                inflater.SetDictionary(dictionary);
            }
            else if (inflater.IsNeedingInput)
            {
                throw new FwxFormatException(name, pos, "поток zlib обрывается на конце файла");
            }
            else if (!inflater.IsFinished)
            {
                throw new FwxFormatException(name, pos, "поток zlib не распаковывается: распаковщик не продвигается");
            }
        }
        return (Encoding.UTF8.GetString(output.ToArray()), data.Length - pos - inflater.RemainingInput);
    }
}
