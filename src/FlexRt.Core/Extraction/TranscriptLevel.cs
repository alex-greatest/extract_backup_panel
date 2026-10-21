namespace FlexRt.Extraction;

/// <summary>Уровень строки текста запуска (<see cref="TranscriptLine"/>).</summary>
public enum TranscriptLevel
{
    /// <summary>Сводка и строка «Файл ПЛК:»: вкладка Info окна.</summary>
    Info,

    /// <summary>Предупреждение («ПРЕДУПРЕЖДЕНИЕ: …»).</summary>
    Warning,

    /// <summary>Ошибка (панель не прочитана, «ОШИБКА: …»).</summary>
    Error
}
