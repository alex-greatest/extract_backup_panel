namespace FlexRt.Export;

/// <summary>Раскладка листа Excel, построенного из готовой таблицы (<see cref="ReportSheet"/>).</summary>
public enum ReportSheetKind
{
    /// <summary>Таблица с первой строки: заголовки, под ними строки.</summary>
    Table,

    /// <summary>Как лист «Сводка»: подпись в A, значение в B, без заголовков.</summary>
    Summary,

    /// <summary>Как лист «Теги»: статистика сверху, таблица — со строки заголовка листа «Теги».</summary>
    Tags
}
