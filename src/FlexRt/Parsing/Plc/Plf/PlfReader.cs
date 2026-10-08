using System.Security.Cryptography;
using FlexRt.Binary;
using FlexRt.Model.Plc.Plf;
using FlexRt.Model.Plc;
using FlexRt.Parsing.Plc.Plf.DataBlocks;
using FlexRt.Parsing.Plc.Plf.Devices;
using FlexRt.Parsing.Plc.Plf.Tags;

namespace FlexRt.Parsing.Plc.Plf;

/// <summary>
/// Каркас PEData.plf проекта TIA: заголовок, цепочка кадров с SHA-256, актуальные версии
/// объектов. Из объектов собирает ПЛК, их теги и блоки данных (<see cref="PlcProject"/>).
/// </summary>
public static class PlfReader
{
    /// <summary>Длина заголовка файла; с этого смещения начинаются кадры.</summary>
    private const int HeaderSize = 0x62;

    /// <summary>Сколько байт заголовка покрывает его SHA-256.</summary>
    private const int HeaderHashedSize = 0x41;

    /// <summary>Длина SHA-256.</summary>
    private const int HashSize = 0x20;

    /// <summary>Класс объекта COMMIT: конец транзакции.</summary>
    private const long CommitClass = 0x00070014;

    /// <summary>Значение <c>+0x0c</c> у надгробия — версии удалённого объекта.</summary>
    private const long DeletedMark = 0xFFFFFFFF;

    /// <summary>
    /// Прочитать проект ПЛК. Проверяет SHA-256 заголовка и каждого кадра, берёт последнюю
    /// версию каждого объекта до последнего COMMIT, удалённые объекты пропускает. Тег, который
    /// не удалось разобрать, попадает в <see cref="PlcProject.Problems"/>, остальные читаются.
    /// Файл открывается на чтение с разрешением записи другим процессам: проект может быть
    /// открыт в TIA.
    /// </summary>
    /// <returns>ПЛК, теги и блоки данных проекта.</returns>
    /// <exception cref="FwxFormatException">Файл повреждён: контрольная сумма, цепочка кадров или нет COMMIT.</exception>
    /// <exception cref="IOException">Файл не удалось прочитать.</exception>
    public static PlcProject Read(string path)
    {
        var b = new FwxBinary(ReadAllShared(path)) { Section = PlfFormat.Section };
        CheckHeader(b);
        var file = new PlfFile(b, LiveObjects(b, ReadFrames(b)));

        var project = new PlcProject();
        project.Devices.AddRange(PlfDeviceParser.Read(file));
        PlfTagParser.ReadAll(file, project);
        PlfDbReader.Read(file, project);
        PlfInterfaceReader.ReadUdts(file, project);
        return project;
    }

    /// <summary>
    /// Прочитать файл целиком одной копией, не мешая TIA, который держит его открытым. Файл
    /// больше 2 ГБ не читается.
    /// </summary>
    /// <returns>Содержимое файла.</returns>
    /// <exception cref="IOException">Файл не удалось прочитать или он больше 2 ГБ.</exception>
    private static byte[] ReadAllShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length > Array.MaxLength)
        {
            throw new IOException($"файл больше {Array.MaxLength} байт");
        }
        var data = new byte[stream.Length];
        stream.ReadExactly(data);
        return data;
    }

    /// <summary>Заголовок: SHA-256 байт <c>0x00..0x40</c> лежит в <c>0x41..0x60</c>.</summary>
    /// <exception cref="FwxFormatException">Файл короче заголовка или сумма не совпала.</exception>
    private static void CheckHeader(FwxBinary b)
    {
        var hash = SHA256.HashData(b.Span(0, HeaderHashedSize));
        if (!b.Span(HeaderHashedSize, HashSize).SequenceEqual(hash))
        {
            throw new FwxFormatException(PlfFormat.Section, HeaderHashedSize, "контрольная сумма заголовка не совпала");
        }
    }

    /// <summary>
    /// Пройти цепочку кадров <c>[u32 size][данные][ff][SHA-256]</c> от конца заголовка.
    /// SHA-256 считается от <c>size</c> байт с начала кадра; следующий кадр — через
    /// <c>size + 0x20</c> байт. Цепочка должна кончиться ровно на конце файла.
    /// </summary>
    /// <returns>Объекты кадров в порядке файла: класс, ID, данные.</returns>
    /// <exception cref="FwxFormatException">Кадр выходит за файл, нет байта <c>ff</c> или сумма не совпала.</exception>
    private static List<PlfObject> ReadFrames(FwxBinary b)
    {
        var frames = new List<PlfObject>();
        long pos = HeaderSize;
        while (pos < b.Length)
        {
            var size = b.D4(pos);
            // минимум: поле длины, класс, ID и байт ff
            if (size < 0xd || pos + size + HashSize > b.Length)
            {
                throw new FwxFormatException(PlfFormat.Section, pos, $"кадр длиной 0x{size:x} выходит за конец файла");
            }
            if (b.D1(pos + size - 1) != 0xff || !b.Span(pos + size, HashSize).SequenceEqual(SHA256.HashData(b.Span(pos, size))))
            {
                throw new FwxFormatException(PlfFormat.Section, pos, "контрольная сумма кадра не совпала");
            }
            frames.Add(new PlfObject(b.D4(pos + 4), b.D4(pos + 8), pos + 4, size - 5));
            pos += size + HashSize;
        }
        return frames;
    }

    /// <summary>
    /// Актуальные объекты: последняя версия каждой пары (класс, ID) среди кадров до
    /// последнего COMMIT включительно. Кадры после него — незавершённая транзакция.
    /// Надгробие (<c>+0x0c</c> = 0xFFFFFFFF) удаляет объект.
    /// </summary>
    /// <returns>Объекты по ключу (класс, ID).</returns>
    /// <exception cref="FwxFormatException">В файле нет ни одного COMMIT.</exception>
    private static Dictionary<(long, long), PlfObject> LiveObjects(FwxBinary b, List<PlfObject> frames)
    {
        var lastCommit = frames.FindLastIndex(f => f.Class == CommitClass);
        if (lastCommit < 0)
        {
            throw new FwxFormatException(PlfFormat.Section, HeaderSize, "в файле нет ни одной завершённой транзакции (COMMIT)");
        }

        var latest = new Dictionary<(long, long), PlfObject>();
        foreach (var frame in frames.Take(lastCommit + 1))
        {
            latest[(frame.Class, frame.Id)] = frame;
        }
        return latest
            .Where(kv => kv.Value.Length < 0x10 || b.D4(kv.Value.Offset + 0x0c) != DeletedMark)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
