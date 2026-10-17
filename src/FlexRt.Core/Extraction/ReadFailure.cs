using FlexRt.Binary;

namespace FlexRt.Extraction;

/// <summary>
/// Различение ошибок шагов запуска (<see cref="ExtractionRun"/>, <see cref="PlcDataReader"/>) и окна: ожидаемая ошибка
/// чтения входа или непредвиденная, и текст ошибки для листа «Ошибки» и окна — один на всех.
/// </summary>
public static class ReadFailure
{
    /// <summary>Ожидаемая ошибка чтения входа: ошибка формата или ввода-вывода, нет доступа к файлу.</summary>
    /// <returns><c>true</c> для <see cref="FwxFormatException"/>, <see cref="IOException"/> и <see cref="UnauthorizedAccessException"/>.</returns>
    public static bool IsReadError(Exception e) => e is FwxFormatException or IOException or UnauthorizedAccessException;

    /// <summary>
    /// Текст ошибки для листа «Ошибки» и окна: ошибки формата и чтения — их сообщение,
    /// непредвиденная — «непредвиденная ошибка &lt;тип&gt;: &lt;сообщение&gt;».
    /// </summary>
    /// <returns>Текст сообщения.</returns>
    public static string Describe(Exception e) => IsReadError(e)
        ? e.Message
        : $"непредвиденная ошибка {e.GetType().Name}: {e.Message}";
}
