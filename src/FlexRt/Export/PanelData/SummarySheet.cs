using ClosedXML.Excel;
using FlexRt.Model.Panel;
using FlexRt.Model.Plc;
using FlexRt.Model.Summary;

namespace FlexRt.Export.PanelData;

/// <summary>Лист «Сводка»: основные сведения о панели, соединениях и ПЛК — подпись в A, значение в B.</summary>
public static class SummarySheet
{
    /// <summary>Текст, если значения нет в прочитанных файлах.</summary>
    private const string Unknown = "неизвестно";

    /// <summary>
    /// Добавить лист «Сводка» в книгу. Строки: панель (файл, модель, версия Runtime, IP и маска,
    /// языки, число строк и тегов), пустая строка, по строке на соединение (имя и IP ПЛК),
    /// пустая строка, данные ПЛК (источник; если прочитаны — по каждому ПЛК имя, модель CPU,
    /// IP из данных ПЛК; число тегов ПЛК и DB). Чего нет в файлах — «неизвестно».
    /// </summary>
    public static void Write(XLWorkbook workbook, FwxDocument doc, PlcProject? project, RunSummary summary)
    {
        var sheet = workbook.Worksheets.Add("Сводка");
        var rows = PanelRows(doc, summary).Append(("", "")).Concat(ConnectionRows(doc)).Append(("", "")).Concat(PlcRows(project, summary));
        var r = 1;
        foreach (var (label, value) in rows)
        {
            if (label.Length > 0)
            {
                sheet.Cell(r, 1).SetValue(label);
                sheet.Cell(r, 2).SetValue(ExcelCell.Text(value));
            }
            r++;
        }
        sheet.Column(1).AdjustToContents();
    }

    /// <summary>Строки панели.</summary>
    /// <returns>Подписи и значения.</returns>
    private static IEnumerable<(string, string)> PanelRows(FwxDocument doc, RunSummary summary)
    {
        var languages = doc.Strings.Select(s => s.Lcid).Distinct().Select(LanguageText.Format).ToList();
        yield return ("Файл панели", summary.PanelPath);
        yield return ("Модель панели", summary.PanelFiles.Model ?? Unknown);
        yield return ("Версия Runtime", summary.PanelFiles.RuntimeVersion ?? Unknown);
        yield return ("IP панели", doc.Devices.Count == 0 ? Unknown : string.Join(", ", doc.Devices.Select(d => $"{IpAddressText.Format(d.Ip)} / {IpAddressText.Format(d.Mask)}")));
        yield return ("Языки панели", languages.Count == 0 ? Unknown : string.Join(", ", languages));
        yield return ("Строк языков", doc.Strings.Count.ToString());
        yield return ("Тегов панели", doc.Tags.Count.ToString());
    }

    /// <summary>Строки соединений: <c>Соединение HMI1</c> — <c>IP ПЛК 192.168.1.1</c>.</summary>
    /// <returns>Подписи и значения; нет соединений — одна строка «нет».</returns>
    private static IEnumerable<(string, string)> ConnectionRows(FwxDocument doc) => doc.Connections.Count == 0
        ? [("Соединения с ПЛК", "нет")]
        : doc.Connections.Select(c => ($"Соединение {c.Name}", $"IP ПЛК {IpAddressText.Format(c.Ip)}"));

    /// <summary>Строки ПЛК: источник данных и, если ПЛК прочитан, сведения по каждому ПЛК и числа тегов и DB.</summary>
    /// <returns>Подписи и значения.</returns>
    private static IEnumerable<(string, string)> PlcRows(PlcProject? project, RunSummary summary)
    {
        yield return ("Данные ПЛК", summary.PlcSource);
        if (project is null)
        {
            yield break;
        }
        foreach (var device in project.Devices)
        {
            yield return ("ПЛК", device.Name ?? Unknown);
            yield return ("Модель CPU", device.Model ?? Unknown);
            yield return ("IP ПЛК (из данных ПЛК)", device.Addresses.Count == 0 ? Unknown : string.Join(", ", device.Addresses.Select(a => IpAddressText.Format(a.Ip))));
        }
        yield return ("Тегов ПЛК", project.Tags.Count.ToString());
        yield return ("Блоков данных (DB)", project.Dbs.Count.ToString());
    }
}
