using Radzen;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>Итог сохранения книги Excel из окна — для уведомления.</summary>
/// <param name="Severity">Вид уведомления: <see cref="NotificationSeverity.Success"/> — записано всё,
/// <see cref="NotificationSeverity.Warning"/> — записано не всё или нечего записывать, <see cref="NotificationSeverity.Error"/> —
/// файл не записан из-за ошибки.</param>
/// <param name="Summary">Заголовок уведомления.</param>
/// <param name="Detail">Путь файла и подробности.</param>
public sealed record SaveOutcome(NotificationSeverity Severity, string Summary, string Detail);
