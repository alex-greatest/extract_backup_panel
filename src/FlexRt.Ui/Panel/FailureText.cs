using System.IO;
using FlexRt.Binary;

namespace FlexRt.Ui.Panel;

/// <summary>Текст ошибки чтения или построения для окна.</summary>
public static class FailureText
{
    /// <summary>Причина ошибки: ошибка формата и ввода-вывода — её сообщение, другая — с типом ошибки.</summary>
    /// <returns>Текст причины без вступления.</returns>
    public static string Describe(Exception e) => e is FwxFormatException or IOException or UnauthorizedAccessException
        ? e.Message
        : $"непредвиденная ошибка {e.GetType().Name}: {e.Message}";
}
