using System.Buffers.Binary;
using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Functions.Scripts;

/// <summary>Последовательное чтение распакованных блоков FUNC_COMMONSCRI. В памяти остаётся один блок.</summary>
internal sealed class ScriptBlockReader
{
    /// <summary>Первое слово записи в проверенных каталогах.</summary>
    private const int RecordMarker = 9;

    /// <summary>Смещение размера хранимой части, отсчитываемого от самого поля.</summary>
    private const int StoredLengthOffset = 0x08;

    /// <summary>Смещение общего размера распакованных данных.</summary>
    private const int DecodedLengthOffset = 0x0c;

    /// <summary>Смещение суммы распакованных байтов по модулю 2³².</summary>
    private const int ChecksumOffset = 0x10;

    /// <summary>Смещение первого заголовка блока.</summary>
    private const int FirstBlockOffset = 0x14;

    /// <summary>Два слова размера блока: в файле и после распаковки.</summary>
    private const int BlockHeaderSize = 0x04;

    /// <summary>Маска расстояния в двухбайтовой ссылке на уже распакованные данные.</summary>
    private const int DistanceMask = 0x0fff;

    /// <summary>Код, при котором длина копирования указана следующим байтом.</summary>
    private const int LongCopyCode = 0x0f;

    /// <summary>Верхняя граница расширения: самая длинная копия даёт 255 байт за три входных байта.</summary>
    private const int MaxExpansion = byte.MaxValue / 3;

    private readonly FwxBinary _binary;
    private readonly string _section;
    private readonly long _checksumOffset;
    private readonly uint _expectedChecksum;
    private readonly long _packedEnd;
    private readonly long _decodedLength;
    private long _nextBlock;
    private byte[] _block = [];
    private int _cursor;
    private long _decoded;
    private uint _checksum;

    /// <summary>Проверить заголовок хранимой части и общий предел распаковки.</summary>
    /// <exception cref="FwxFormatException">Заголовок или объявленные границы записи повреждены.</exception>
    public ScriptBlockReader(FwxBinary binary, TocEntry table, TableItem item)
    {
        _binary = binary;
        _section = table.Name;
        _checksumOffset = item.Offset + ChecksumOffset;
        _nextBlock = item.Offset + FirstBlockOffset;
        BlockOffset = _nextBlock;

        if (item.Length < FirstBlockOffset || binary.D4(item.Offset) != RecordMarker || binary.D4(item.Offset + 0x04) != 0)
        {
            throw Error(item.Offset, "неизвестный заголовок каталога VB");
        }
        var storedLength = binary.D4(item.Offset + StoredLengthOffset);
        _packedEnd = item.Offset + StoredLengthOffset + storedLength;
        var padding = item.Offset + item.Length - _packedEnd;
        if (_packedEnd <= _nextBlock || padding is < 0 or >= 4)
        {
            throw Error(item.Offset + StoredLengthOffset, "границы сжатых данных не совпадают с длиной записи");
        }
        for (var offset = _packedEnd; offset < item.Offset + item.Length; offset++)
        {
            if (binary.D1(offset) != 0)
            {
                throw Error(offset, "ненулевой байт выравнивания сжатых данных");
            }
        }

        _decodedLength = binary.D4(item.Offset + DecodedLengthOffset);
        if (_decodedLength <= 0 || _decodedLength > (_packedEnd - _nextBlock) * MaxExpansion)
        {
            throw Error(item.Offset + DecodedLengthOffset, "невозможный размер распакованных данных");
        }
        _expectedChecksum = (uint)binary.D4(_checksumOffset);
    }

    /// <summary>Смещение заголовка текущего блока в FWC.</summary>
    public long BlockOffset { get; private set; }

    /// <summary>Число ещё не прочитанных байтов по объявленному размеру.</summary>
    public long Remaining => _decodedLength - _decoded + _block.Length - _cursor;

    /// <summary>Прочитать слово u16, при необходимости из двух блоков.</summary>
    /// <returns>Значение слова.</returns>
    /// <exception cref="FwxFormatException">Слово выходит за данные или повреждён блок.</exception>
    public ushort ReadWord()
    {
        Span<byte> word = stackalloc byte[sizeof(ushort)];
        Read(word);
        return BinaryPrimitives.ReadUInt16LittleEndian(word);
    }

    /// <summary>Прочитать слово u32, при необходимости из двух блоков.</summary>
    /// <returns>Значение слова.</returns>
    /// <exception cref="FwxFormatException">Слово выходит за данные или повреждён блок.</exception>
    public uint ReadDoubleWord()
    {
        Span<byte> word = stackalloc byte[sizeof(uint)];
        Read(word);
        return BinaryPrimitives.ReadUInt32LittleEndian(word);
    }

    /// <summary>Прочитать распакованные байты в переданный буфер.</summary>
    /// <exception cref="FwxFormatException">Байты выходят за данные или повреждён блок.</exception>
    public void Read(Span<byte> target)
    {
        CheckLength(target.Length);
        while (!target.IsEmpty)
        {
            var bytes = ReadChunk(target.Length);
            bytes.CopyTo(target);
            target = target[bytes.Length..];
        }
    }

    /// <summary>Пропустить байты без сохранения их содержимого.</summary>
    /// <exception cref="FwxFormatException">Байты выходят за данные или повреждён блок.</exception>
    public void Skip(long count)
    {
        CheckLength(count);
        while (count > 0)
        {
            count -= ReadChunk(count).Length;
        }
    }

    /// <summary>Проверить оставшиеся блоки, общую длину и контрольную сумму.</summary>
    /// <exception cref="FwxFormatException">Блоки повреждены, их длина или контрольная сумма не сходится.</exception>
    public void Finish()
    {
        Skip(Remaining);
        if (_nextBlock != _packedEnd)
        {
            throw Error(_nextBlock, "после последнего блока остались сжатые данные");
        }
        if (_checksum != _expectedChecksum)
        {
            throw Error(_checksumOffset, "не совпадает контрольная сумма распакованных данных");
        }
    }

    /// <summary>Проверить, что чтение помещается в объявленный объём распакованных данных.</summary>
    /// <exception cref="FwxFormatException">Чтение выходит за объявленные данные.</exception>
    private void CheckLength(long count)
    {
        if (count < 0 || count > Remaining)
        {
            throw Error(BlockOffset, "чтение выходит за распакованные данные");
        }
    }

    /// <summary>Взять до указанного числа байтов из одного блока и сдвинуть позицию.</summary>
    /// <returns>Часть текущего блока; на его границе распаковывается следующий.</returns>
    /// <exception cref="FwxFormatException">Следующий блок повреждён.</exception>
    private ReadOnlySpan<byte> ReadChunk(long maximum)
    {
        EnsureBlock();
        var count = (int)Math.Min(maximum, _block.Length - _cursor);
        var bytes = _block.AsSpan(_cursor, count);
        _cursor += count;
        return bytes;
    }

    /// <summary>Распаковать следующий блок, если текущий полностью прочитан.</summary>
    /// <exception cref="FwxFormatException">Повреждён заголовок или содержимое блока.</exception>
    private void EnsureBlock()
    {
        if (_cursor < _block.Length)
        {
            return;
        }

        BlockOffset = _nextBlock;
        var start = BlockOffset + BlockHeaderSize;
        if (start > _packedEnd)
        {
            throw Error(BlockOffset, "не помещается заголовок сжатого блока");
        }
        var packedSize = _binary.D2(BlockOffset);
        var decodedSize = _binary.D2(BlockOffset + 0x02);
        if (packedSize == 0 || packedSize > _packedEnd - start || decodedSize == 0
            || decodedSize > _decodedLength - _decoded || decodedSize > packedSize * MaxExpansion)
        {
            throw Error(BlockOffset, "неверные размеры сжатого блока");
        }
        _block = Decode(_binary.Span(start, packedSize), decodedSize, start);
        _cursor = 0;
        _nextBlock = start + packedSize;
        _decoded += decodedSize;
        foreach (var value in _block)
        {
            _checksum = unchecked(_checksum + value);
        }
    }

    /// <summary>Распаковать блок. Биты флага от старшего к младшему выбирают байт или ссылку.</summary>
    /// <returns>Распакованные байты блока.</returns>
    /// <exception cref="FwxFormatException">Ссылка, длина или границы блока повреждены.</exception>
    private byte[] Decode(ReadOnlySpan<byte> source, int length, long start)
    {
        var block = new byte[length];
        var read = 0;
        var written = 0;
        while (read < source.Length)
        {
            var flagOffset = start + read;
            var flags = source[read++];
            if (read == source.Length)
            {
                throw Error(flagOffset, "за байтом флагов блока нет данных");
            }
            for (var bit = 0; bit < 8 && read < source.Length; bit++)
            {
                if (written == block.Length)
                {
                    throw Error(start + read, "данные выходят за распакованный блок");
                }
                if ((flags & 0x80) == 0)
                {
                    block[written++] = source[read++];
                }
                else
                {
                    Copy(source, block, start, ref read, ref written);
                }
                flags = unchecked((byte)(flags << 1));
            }
            if (flags != 0)
            {
                throw Error(flagOffset, "у последнего байта флагов блока есть ссылка без данных");
            }
        }
        return written == length ? block : throw Error(start + read, "распакованный блок короче объявленного");
    }

    /// <summary>Скопировать байты из уже распакованной части того же блока, включая перекрытие.</summary>
    /// <exception cref="FwxFormatException">Ссылка не помещается, ссылается за начало либо даёт выход из блока.</exception>
    private void Copy(ReadOnlySpan<byte> source, Span<byte> block, long start, ref int read, ref int written)
    {
        var offset = start + read;
        if (source.Length - read < sizeof(ushort))
        {
            throw Error(offset, "не помещается ссылка на распакованные байты");
        }
        var token = BinaryPrimitives.ReadUInt16LittleEndian(source[read..]);
        read += sizeof(ushort);
        var distance = token & DistanceMask;
        var code = token >> 12;
        var length = code == LongCopyCode
            ? read < source.Length ? source[read++] : throw Error(offset, "не помещается длина копии")
            : code + 2;
        if (distance == 0 || distance > written || length == 0 || length > block.Length - written)
        {
            throw Error(offset, "неверное расстояние или длина копии");
        }
        for (var index = 0; index < length; index++)
        {
            block[written] = block[written - distance];
            written++;
        }
    }

    /// <summary>Сформировать ошибку с абсолютным смещением в сжатой записи.</summary>
    /// <returns>Ошибка формата FWX.</returns>
    private FwxFormatException Error(long offset, string message) => new(_section, offset, message);
}
