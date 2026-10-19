using FlexRt.Extraction;

namespace FlexRt.Ui.Panel.Saving;

/// <summary>Итог сохранения книги Excel из окна — для уведомления.</summary>
/// <param name="Level"><see cref="TranscriptLevel.Info"/> — записано всё, <see cref="TranscriptLevel.Warning"/> — записано
/// не всё или нечего записывать, <see cref="TranscriptLevel.Error"/> — файл не записан из-за ошибки.</param>
/// <param name="Summary">Заголовок уведомления.</param>
/// <param name="Detail">Путь файла и подробности.</param>
public sealed record SaveOutcome(TranscriptLevel Level, string Summary, string Detail);
