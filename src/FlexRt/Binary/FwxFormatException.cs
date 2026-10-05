namespace FlexRt.Binary;

/// <summary>Ошибка формата FWX: секция и смещение, на котором разбор сломался.</summary>
/// <param name="section">Таблица или часть файла, в которой произошла ошибка.</param>
/// <param name="offset">Смещение от начала файла.</param>
/// <param name="message">Что именно не так; в текст исключения добавляются секция и смещение.</param>
public sealed class FwxFormatException(string section, long offset, string message)
    : Exception($"{section} @ 0x{offset:X}: {message}")
{
    /// <summary>Таблица или часть файла, в которой произошла ошибка.</summary>
    public string Section { get; } = section;

    /// <summary>Смещение от начала файла, на котором произошла ошибка.</summary>
    public long Offset { get; } = offset;
}
