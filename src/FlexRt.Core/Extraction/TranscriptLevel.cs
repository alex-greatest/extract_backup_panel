namespace FlexRt.Extraction;

/// <summary>Уровень строки текста запуска (<see cref="TranscriptLine"/>).</summary>
public enum TranscriptLevel
{
    /// <summary>Сводка и итоги: раньше печатались консолью в stdout.</summary>
    Info,

    /// <summary>Предупреждение («ПРЕДУПРЕЖДЕНИЕ: …»): раньше печаталось консолью в stderr.</summary>
    Warning,

    /// <summary>Ошибка (панель не прочитана, «ОШИБКА: …», ошибка экспорта): раньше печаталась консолью в stderr.</summary>
    Error
}
