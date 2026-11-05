using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Пользователи Utah V 1.4 PWL file из PWX/PWL. Хеши и соль проверяются по границам, но не возвращаются.</summary>
public static class UserFileParser
{
    /// <summary>Первая запись после заголовка в проверенных Project1, CCB-A13 и CSM1_CP1.</summary>
    private const int RecordsOffset = 0x3c;

    /// <summary>Заголовок расшифрованного формата, включая завершающий ноль.</summary>
    private const string Signature = "Utah V 1.4 PWL file\0";

    /// <summary>Прочитать все записи до конца файла; при ошибке частичный список не возвращается.</summary>
    /// <returns>Пользователи в порядке файла.</returns>
    /// <exception cref="IOException">Файл не удалось прочитать.</exception>
    /// <exception cref="UnauthorizedAccessException">Нет доступа к файлу.</exception>
    /// <exception cref="FwxFormatException">Незнакомый заголовок или запись выходит за файл.</exception>
    public static IReadOnlyList<PanelUser> Read(string path)
    {
        var binary = new FwxBinary(UserFileCipher.Decode(File.ReadAllBytes(path))) { Section = "PWL" };
        binary.Check(0, RecordsOffset);
        // В проверенных файлах перед именем формата — u32 10, его назначение не установлено.
        if (binary.D4(0x08) != 10 || binary.GetNameLen(0x0c, Signature.Length) != Signature)
        {
            throw new FwxFormatException("PWL", 0x08, "ожидался заголовок Utah V 1.4 PWL file");
        }
        List<PanelUser> users = [];
        long position = RecordsOffset;
        while (position < binary.Length)
        {
            users.Add(ReadUser(binary, ref position));
        }
        return users;
    }

    /// <summary>Имя занимает заданное число u32; за ним группа, автовыход, восемь служебных байт и число блоков пароля.</summary>
    /// <returns>Имя, номер группы и время; назначение служебных байтов не установлено.</returns>
    /// <exception cref="FwxFormatException">Имя, поля или блоки пароля не помещаются в файл.</exception>
    private static PanelUser ReadUser(FwxBinary binary, ref long position)
    {
        var nameSize = binary.D4(position) * sizeof(uint);
        position += sizeof(uint);
        if (nameSize == 0)
        {
            throw new FwxFormatException("PWL", position - sizeof(uint), "пустая область имени пользователя");
        }
        binary.Check(position, nameSize + 0x14);
        var name = binary.GetNameLen(position, checked((int)(nameSize / sizeof(char))));
        var terminator = name.IndexOf('\0');
        if (terminator < 0)
        {
            throw new FwxFormatException("PWL", position, "имя пользователя без завершающего нуля");
        }
        position += nameSize;
        var user = new PanelUser(name[..terminator], binary.D4(position), binary.D4(position + 4));
        var blocks = binary.D4(position + 0x10);
        position += 0x14;
        for (var index = 0L; index < blocks; index++)
        {
            SkipPassword(binary, ref position);
        }
        return user;
    }

    /// <summary>Пропустить блок: код, длина соли, длина хеша, число итераций, соль и хеш; данные пароля не сохраняются.</summary>
    /// <exception cref="FwxFormatException">Заголовок или содержимое блока выходит за файл.</exception>
    private static void SkipPassword(FwxBinary binary, ref long position)
    {
        binary.Check(position, 0x10);
        var length = binary.D4(position + 4) + binary.D4(position + 8);
        position += 0x10;
        binary.Check(position, length);
        position += length;
    }
}
