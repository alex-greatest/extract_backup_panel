using System.Text.RegularExpressions;
using FlexRt.Model.Panel.Alarms;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Текст дискретного или аналогового аларма с полями вывода в виде, как в колонке Alarm text в TIA.</summary>
public static partial class AlarmText
{
    /// <summary>
    /// Метка поля в тексте из STRINGSTORE: ESC, <c>T</c>, номер поля с единицы одним символом и
    /// символ 0xFF (наблюдение на A603A0097: <c>1B 54 01 FF</c>).
    /// </summary>
    [GeneratedRegex("\eT(.)ÿ", RegexOptions.Singleline)]
    private static partial Regex FieldMark();

    /// <summary>
    /// Подписи полей аларма: поле тега — <c>&lt;Tag : 6, DB_ST7_Axis_Axis_Receive_FAULT_CODE&gt;</c>
    /// (длина и имя HMI-тега; тег не найден — <c>?</c>), поле текстового списка —
    /// <c>&lt;Text list, ?&gt;</c>: имени списка нет ни в панели, ни в ПЛК. Вид сверен с TIA на
    /// A603A0097 (алармы 30 и 70).
    /// </summary>
    /// <returns>Подписи по порядку полей.</returns>
    public static List<string> FieldLabels(IEnumerable<AlarmField> fields, AlarmFieldNames names) =>
    [
        .. fields.Select(f => f.TextList ? "<Text list, ?>" : $"<Tag : {f.Length}, {names.Find(f)}>")
    ];

    /// <summary>
    /// Текст с подписями полей вместо меток. Метка с номером, которого нет среди полей, —
    /// <c>&lt;?&gt;</c>. Остальной текст — как есть.
    /// </summary>
    /// <returns>Текст ячейки.</returns>
    public static string Format(string text, IReadOnlyList<string> labels) =>
        FieldMark().Replace(text, mark => Label(mark, labels));

    /// <summary>Подпись поля для одной метки: по номеру поля из метки.</summary>
    /// <returns>Подпись или <c>&lt;?&gt;</c>, если поля с таким номером нет.</returns>
    private static string Label(Match mark, IReadOnlyList<string> labels)
    {
        var number = mark.Groups[1].Value[0];
        return number >= 1 && number <= labels.Count ? labels[number - 1] : "<?>";
    }
}
