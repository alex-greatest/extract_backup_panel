using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Users;

/// <summary>
/// Таблица листа «Группы пользователей» (решение пользователя 2026-11-03): <c>Number</c>, <c>Display name</c> на каждом
/// языке панели, затем колонка на право — <c>Yes</c>, если право у группы есть, иначе пусто.
/// </summary>
public static class UserGroupsTable
{
    /// <summary>Начало заголовков колонок Display name, за ним язык.</summary>
    private const string DisplayNamePrefix = "Display name";

    /// <summary>Отметка права у группы, как Start screen на листе «Экраны».</summary>
    private const string Granted = "Yes";

    /// <summary>
    /// Стандартные права TIA по биту маски: имён прав в файле нет, имена — допущение о стандартных правах (решение
    /// пользователя 2026-11-03). Сверено с TIA: бит 1 — Monitor, бит 2 — Operate (шаги 68–69).
    /// </summary>
    private static readonly string[] StandardRights = ["User administration", "Monitor", "Operate"];

    /// <summary>Наибольшее число битов маски прав (u32).</summary>
    private const int MaxRights = 32;

    /// <summary>
    /// Построить таблицу: строка на группу в порядке файла. Колонки прав — три стандартных всегда, свои права (бит 3 и
    /// выше) — только если они есть хотя бы у одной группы: <c>Право N</c>, N — номер права в TIA (бит + 1).
    /// </summary>
    /// <returns>Заголовки с языками и строки групп.</returns>
    public static ReportTable Build(FwxDocument doc)
    {
        var strings = LanguageStrings.Of(doc);
        var rights = RightBits(doc);
        List<ReportColumn> columns =
        [
            new("Number"),
            .. strings.Columns(DisplayNamePrefix),
            .. rights.Select(bit => new ReportColumn(RightName(bit)))
        ];
        var rows = doc.UserGroups
            .Select(g => (ReportCell[])
            [
                g.Number,
                .. strings.Cells(g.TextIndex),
                .. rights.Select(bit => (ReportCell)(Has(g.Rights, bit) ? Granted : ""))
            ])
            .ToList();
        return ReportTable.WithLanguages(columns, rows);
    }

    /// <summary>Биты прав для колонок: три стандартных и биты выше, которые есть хотя бы у одной группы.</summary>
    /// <returns>Номера битов по возрастанию.</returns>
    private static List<int> RightBits(FwxDocument doc)
    {
        var any = doc.UserGroups.Aggregate(0L, (mask, g) => mask | g.Rights);
        return [.. Enumerable.Range(0, MaxRights).Where(bit => bit < StandardRights.Length || Has(any, bit))];
    }

    /// <summary>Есть ли в маске прав бит <paramref name="bit"/>.</summary>
    /// <returns><c>true</c> — право есть.</returns>
    private static bool Has(long rights, int bit) => (rights >> bit & 1) != 0;

    /// <summary>Заголовок колонки права: имя стандартного права или <c>Право N</c>.</summary>
    /// <returns>Заголовок.</returns>
    private static string RightName(int bit) => bit < StandardRights.Length ? StandardRights[bit] : $"Право {bit + 1}";
}
