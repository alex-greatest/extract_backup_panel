using FlexRt.Model.Panel;

namespace FlexRt.Export.PanelData.Alarms;

/// <summary>Таблица листа «Группы алармов»: группы, назначенные дискретным и аналоговым алармам, и число алармов в каждой.</summary>
public static class AlarmGroupsTable
{
    /// <summary>Заголовки колонок.</summary>
    private static readonly string[] Columns = ["ID", "Discrete alarms", "Analog alarms"];

    /// <summary>
    /// Группы, на которые ссылаются алармы панели, по возрастанию ID, с числом дискретных и
    /// аналоговых алармов в каждой. Аларм без группы (0) не учитывается. Группа, не назначенная
    /// ни одному аларму, в панель не попадает (наблюдение на Project1), поэтому список может быть
    /// короче списка Alarm groups в TIA; имени группы в панели нет.
    /// </summary>
    /// <returns>ID группы и число алармов каждого вида.</returns>
    public static IReadOnlyList<(int Id, int Discrete, int Analog)> Groups(FwxDocument doc) =>
        doc.DiscreteAlarms.Select(a => a.Group)
            .Concat(doc.AnalogAlarms.Select(a => a.Group))
            .Where(g => g != 0)
            .Distinct()
            .Order()
            .Select(g => (g, doc.DiscreteAlarms.Count(a => a.Group == g), doc.AnalogAlarms.Count(a => a.Group == g)))
            .ToList();

    /// <summary>Построить таблицу: <c>ID</c>, <c>Discrete alarms</c>, <c>Analog alarms</c>, строка на группу из <see cref="Groups"/>.</summary>
    /// <returns>Заголовки и строки групп.</returns>
    public static ReportTable Build(FwxDocument doc)
    {
        var rows = Groups(doc).Select(g => new ReportCell[] { g.Id, g.Discrete, g.Analog }).ToList();
        return new ReportTable(Columns, rows);
    }
}
