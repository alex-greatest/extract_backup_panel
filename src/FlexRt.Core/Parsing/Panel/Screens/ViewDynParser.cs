using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Разбор VIEW_DYN: высота Permanent area панели.</summary>
public static class ViewDynParser
{
    /// <summary>
    /// Смещение u16 высоты Permanent area от конца ссылок (+0x16 от начала элемента при одной ссылке). Перед ней
    /// u16 0, u16 0x5000 и u16 0 (гипотеза: левая граница рабочей области), за ней u16 ширина и u16 высота панели
    /// (наблюдение) — не читаются.
    /// </summary>
    private const int PermanentAreaHeightOffset = 0x06;

    /// <summary>Номер записи <c>Overview</c> (объекты Permanent area) в таблице SCREEN, с нуля.</summary>
    private const int OverviewIndex = 0;

    /// <summary>
    /// Прочитать элемент VIEW_DYN, на который ссылается объект записи <c>Overview</c> таблицы SCREEN (в файлах
    /// элемент один, ссылка — на него), и записать высоту Permanent area в
    /// <see cref="FwxDocument.PermanentAreaHeight"/>: начало со ссылками (<see cref="ReferenceHeader"/>), за ними на
    /// <see cref="PermanentAreaHeightOffset"/> — u16 (сверено с Height Permanent area в TIA: шаг
    /// <c>data/real/steps-screen-props/15_overview_height.fwc</c>, 60 → 80; у панелей без Permanent area — 0).
    /// Нет таблицы в TOC или записи <c>Overview</c> в <see cref="FwxDocument.Screens"/> (SCREEN нет или её разбор
    /// оборвался раньше — об этом уже предупреждает SCREEN) — ничего не делает, высота не известна. Разбирается после SCREEN.
    /// Читается только этот элемент: записи каталога смещений и границы других элементов не проверяются (их проверяет
    /// <see cref="TableLayoutCheck"/>).
    /// </summary>
    /// <exception cref="FwxFormatException">
    /// Запись <c>Overview</c> не ссылается на VIEW_DYN, ссылка ведёт за последний элемент, запись каталога смещений
    /// элемента выходит за файл или даёт отрицательную длину, элемент выходит за пределы таблицы или не сходится с раскладкой.
    /// </exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("VIEW_DYN");
        var overview = doc.Screens.FirstOrDefault(s => s is { Index: OverviewIndex, IsService: false });
        if (table is null || overview is null)
        {
            return;
        }

        var item = FwxReader.CheckedItem(doc.Binary, table, OverviewElement(overview, table));
        doc.PermanentAreaHeight = ReferenceHeader.ReadU16After(doc.Binary, item, PermanentAreaHeightOffset, "высота Permanent area");
    }

    /// <summary>Номер элемента VIEW_DYN из первой пары объектов записи <c>Overview</c> с Id этой таблицы.</summary>
    /// <returns>Номер элемента с нуля, меньше числа элементов таблицы.</returns>
    /// <exception cref="FwxFormatException">Ссылки нет или она ведёт за последний элемент таблицы.</exception>
    private static int OverviewElement(Screen overview, TocEntry table)
    {
        var index = overview.Objects.Where(o => o.Table == table.Id).Select(o => (int?)o.Index).FirstOrDefault();
        if (index is not { } element)
        {
            throw new FwxFormatException(table.Name, table.Offset, "запись Overview таблицы SCREEN не ссылается на VIEW_DYN, высота Permanent area не известна");
        }
        return element < table.Entries
            ? element
            : throw new FwxFormatException(table.Name, table.Offset, $"запись Overview ссылается на элемент {element + 1}, а элементов {table.Entries}");
    }
}
