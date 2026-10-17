namespace FlexRt.Extraction;

/// <summary>Строка текста запуска (<see cref="RunTranscript"/>).</summary>
/// <param name="Level">Уровень строки.</param>
/// <param name="Text">Текст строки без перевода строки.</param>
public sealed record TranscriptLine(TranscriptLevel Level, string Text);
