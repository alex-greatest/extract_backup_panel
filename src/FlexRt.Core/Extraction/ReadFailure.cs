using FlexRt.Binary;

namespace FlexRt.Extraction;

/// <summary>
/// Различение ошибок шагов окна (<see cref="PlcDataReader"/>, открытие и сохранение): ожидаемая ошибка чтения входа
/// или непредвиденная, и текст ошибки для окна — один на всех.
/// </summary>
public static class ReadFailure
{
    /// <summary>Ожидаемая ошибка чтения входа: ошибка формата или ввода-вывода, нет доступа к файлу.</summary>
    /// <returns><c>true</c> для <see cref="FwxFormatException"/>, <see cref="IOException"/> и <see cref="UnauthorizedAccessException"/>.</returns>
    private static bool IsReadError(Exception e) => e is FwxFormatException or IOException or UnauthorizedAccessException;

    /// <summary>
    /// Текст ошибки для окна: ошибки формата и чтения — их сообщение,
    /// непредвиденная — «непредвиденная ошибка &lt;тип&gt;: &lt;сообщение&gt;».
    /// </summary>
    /// <returns>Текст сообщения.</returns>
    public static string Describe(Exception e) => IsReadError(e)
        ? e.Message
        : $"непредвиденная ошибка {e.GetType().Name}: {e.Message}";
}
