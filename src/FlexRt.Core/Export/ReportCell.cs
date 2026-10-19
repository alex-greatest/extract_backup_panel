using System.Globalization;

namespace FlexRt.Export;

/// <summary>
/// Значение ячейки таблицы отчёта: текст или число и, если нужно, цвет заливки. Не зависит от Excel: те же таблицы может
/// показать и окно программы. Пустой текст — пустая ячейка.
/// </summary>
public readonly record struct ReportCell
{
    /// <summary>Текст ячейки; <c>null</c>, если ячейка — число.</summary>
    public string? Text { get; }

    /// <summary>Число ячейки; <c>null</c>, если ячейка — текст.</summary>
    public long? Number { get; }

    /// <summary>
    /// Ячейка строкой — для показа в окне, копирования и «Сводки», где значения строки: число — в инвариантной культуре,
    /// текст — как есть, пустая ячейка — пустая строка.
    /// </summary>
    public string Display => Number?.ToString(CultureInfo.InvariantCulture) ?? Text ?? "";

    /// <summary>Цвет заливки ячейки <c>0xRRGGBB</c>; <c>null</c> — без заливки.</summary>
    public int? FillRgb { get; init; }

    /// <summary>Создаёт ячейку; ровно одно из значений не <c>null</c>.</summary>
    private ReportCell(string? text, long? number)
    {
        Text = text;
        Number = number;
    }

    /// <summary>Текстовая ячейка.</summary>
    public static implicit operator ReportCell(string text) => new(text, null);

    /// <summary>Числовая ячейка.</summary>
    public static implicit operator ReportCell(long number) => new(null, number);
}
