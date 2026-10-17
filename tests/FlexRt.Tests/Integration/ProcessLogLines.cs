using FlexRt.Extraction;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Что должно быть в логе процесса для строки текста запуска (<see cref="RunTranscript"/>): лог пишет те же события
/// своими словами (<c>LogReporter</c>). Так тесты видят, что консольная программа не потеряла предупреждения, ошибки,
/// строку данных ПЛК и итоги экспортов, хотя ничего не печатает.
/// </summary>
public static class ProcessLogLines
{
    /// <summary>Начало строки предупреждения разбора панели в тексте запуска.</summary>
    private const string NotParsed = "ПРЕДУПРЕЖДЕНИЕ: не разобрано - ";

    /// <summary>Начало строки проблемы данных ПЛК в тексте запуска.</summary>
    private const string PlcProblem = "ПРЕДУПРЕЖДЕНИЕ: файл ПЛК - ";

    /// <summary>Начало строки таблицы, сохранённой целиком, в тексте запуска.</summary>
    private const string RawTable = "ПРЕДУПРЕЖДЕНИЕ: таблица сохранена целиком (raw.hex.txt) - ";

    /// <summary>Начало строки «не поддерживается» в тексте запуска.</summary>
    private const string Unsupported = "ОШИБКА: ";

    /// <summary>Начало строки панели, которая не прочитана, в тексте запуска.</summary>
    private const string PanelNotRead = "Не удалось прочитать ";

    /// <summary>Начало строки данных ПЛК в тексте запуска.</summary>
    private const string PlcFile = "Файл ПЛК: ";

    /// <summary>
    /// Фрагмент лога для строки текста запуска. Предупреждения, ошибки, строка «Файл ПЛК:» и итоги экспортов в логе есть
    /// всегда; сводка панели (таблицы TOC, числа) пишется в лог одной строкой, её строки не сверяются.
    /// </summary>
    /// <param name="line">Строка текста запуска.</param>
    /// <returns>Фрагмент, который должен быть в логе; <c>null</c> — строка не сверяется.</returns>
    /// <exception cref="InvalidOperationException">Предупреждение или ошибка без правила сверки — правило нужно добавить.</exception>
    public static string? Expected(TranscriptLine line) => line.Level == TranscriptLevel.Info ? ForInfo(line.Text) : ForProblem(line.Text);

    /// <summary>Фрагмент лога для обычной строки: «Файл ПЛК:» и итог экспорта; остальные не сверяются.</summary>
    /// <returns>Фрагмент лога или <c>null</c>.</returns>
    private static string? ForInfo(string text)
    {
        if (text.StartsWith(PlcFile, StringComparison.Ordinal))
        {
            return PlcFileLog(text[PlcFile.Length..]);
        }
        // итог экспорта «[название] итог» в логе — с временем в скобках
        return text.StartsWith('[') ? $"{text} (" : null;
    }

    /// <summary>
    /// Фрагмент лога для строки «Файл ПЛК: …»: не найден, не прочитан или прочитан (тогда в логе время чтения, а не
    /// числа).
    /// </summary>
    /// <returns>Фрагмент лога.</returns>
    private static string PlcFileLog(string rest)
    {
        const string notFound = " не найден - данные ПЛК: неизвестно";
        const string notRead = " не прочитан - данные ПЛК: неизвестно";
        if (rest.EndsWith(notFound, StringComparison.Ordinal))
        {
            return $"Файл ПЛК {rest[..^notFound.Length]} не найден";
        }
        return rest.EndsWith(notRead, StringComparison.Ordinal)
            ? $"Файл ПЛК {rest[..^notRead.Length]} не прочитан"
            : "Проект ПЛК прочитан за ";
    }

    /// <summary>Фрагмент лога для предупреждения или ошибки.</summary>
    /// <returns>Фрагмент лога.</returns>
    /// <exception cref="InvalidOperationException">Для строки нет правила сверки.</exception>
    private static string ForProblem(string text)
    {
        if (text.StartsWith(NotParsed, StringComparison.Ordinal))
        {
            return $"Не разобрано: {text[NotParsed.Length..]}";
        }
        if (text.StartsWith(PlcProblem, StringComparison.Ordinal))
        {
            return $"Файл ПЛК: {text[PlcProblem.Length..]}";
        }
        if (text.StartsWith(RawTable, StringComparison.Ordinal))
        {
            return $"Таблица сохранена целиком (raw.hex.txt): {text[RawTable.Length..]}";
        }
        if (text.StartsWith(Unsupported, StringComparison.Ordinal))
        {
            return $"Запуск прекращён: {text[Unsupported.Length..]}";
        }
        return PanelReadFailure(text) ?? ExportFailure(text)
            ?? throw new InvalidOperationException($"нет правила сверки с логом для строки «{text}»");
    }

    /// <summary>
    /// Фрагмент лога для «Не удалось прочитать путь: сообщение»: в логе — путь, сообщение — в стеке ошибки.
    /// </summary>
    /// <returns>«Не удалось прочитать панель путь» или <c>null</c>, если строка не такая.</returns>
    private static string? PanelReadFailure(string text)
    {
        if (!text.StartsWith(PanelNotRead, StringComparison.Ordinal))
        {
            return null;
        }
        var end = text.IndexOf(": ", PanelNotRead.Length, StringComparison.Ordinal);
        return end > 0 ? $"Не удалось прочитать панель {text[PanelNotRead.Length..end]}" : null;
    }

    /// <summary>Фрагмент лога для ошибки экспорта «[название] ОШИБКА: сообщение».</summary>
    /// <returns>«[название] ошибка экспорта» или <c>null</c>, если строка не такая.</returns>
    private static string? ExportFailure(string text)
    {
        var end = text.IndexOf("] ОШИБКА: ", StringComparison.Ordinal);
        return text.StartsWith('[') && end > 0 ? $"{text[..(end + 1)]} ошибка экспорта" : null;
    }
}
