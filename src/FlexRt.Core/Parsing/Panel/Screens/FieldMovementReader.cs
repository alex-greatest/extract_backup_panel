using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Ссылки движения VIEW_MOVE: VAR для Direct либо VIEW_MOVE_ADAPT для движения по диапазону.</summary>
internal static class FieldMovementReader
{
    /// <summary>Второе служебное слово ссылки X в VIEW_MOVE; для Y — 0x1a (опыты 14–17).</summary>
    private const int XAxis = 0x19;

    /// <summary>Второе служебное слово ссылки Y в VIEW_MOVE.</summary>
    private const int YAxis = 0x1a;

    /// <summary>Размер тела VIEW_MOVE_ADAPT: два u32, две f64, четыре i16 координат.</summary>
    private const int BodySize = 0x20;

    /// <summary>Прочитать движение; обычная группа возвращает null, повреждённая ссылка движения — непрочитанное движение.</summary>
    /// <returns>Настройки движения; сбой — IsRead false и предупреждение.</returns>
    public static FieldMovement? Read(FwxDocument doc, TableItem wrapper, IReadOnlyList<VarReference> links)
    {
        try
        {
            CheckTargets(doc, wrapper, links);
            var adapter = links.FirstOrDefault(l => doc.FindTable(l.Table)?.Name == "VIEW_MOVE_ADAPT");
            if (adapter.Table != 0)
            {
                return Adapted(doc, adapter);
            }
            var axes = links.Select((link, index) => (Link: link, Axis: doc.Binary.D2(wrapper.Offset + 0x0e + index * 8L)))
                .Where(l => l.Axis is XAxis or YAxis && doc.FindTable(l.Link.Table)?.Name == "VAR").ToArray();
            if (axes.Length == 0)
            {
                return null;
            }
            return new FieldMovement(true,
                axes.Where(a => a.Axis == XAxis).Select(a => Tag(doc, a.Link, "VIEW_MOVE", wrapper.Offset)).FirstOrDefault(),
                axes.Where(a => a.Axis == YAxis).Select(a => Tag(doc, a.Link, "VIEW_MOVE", wrapper.Offset)).FirstOrDefault(),
                null, null, null, null, null, null);
        }
        catch (FwxFormatException e)
        {
            doc.Warnings.Add($"{e.Message} (движение поля не прочитано)");
            return new FieldMovement(false, null, null, null, null, null, null, null, null, false);
        }
    }

    /// <summary>Проверить ненулевые цели ссылок X/Y; Id 0 у неиспользуемой оси остаётся допустимым.</summary>
    /// <exception cref="FwxFormatException">Таблица цели отсутствует или номер элемента движения вне таблицы.</exception>
    private static void CheckTargets(FwxDocument doc, TableItem wrapper, IReadOnlyList<VarReference> links)
    {
        for (var i = 0; i < links.Count; i++)
        {
            var offset = wrapper.Offset + 0x08 + i * 8L;
            var axis = doc.Binary.D2(offset + 0x06);
            if (links[i].Table == 0 || axis is not (XAxis or YAxis))
            {
                continue;
            }
            var table = AnimationLinkReader.Table(doc, links[i], "VIEW_MOVE", offset);
            if (table.Name is "VAR" or "VIEW_MOVE_ADAPT")
            {
                ElementLink.Item(doc, table, links[i].Index);
            }
        }
    }

    /// <summary>Движение по диапазону: два f64 с +8 и координаты с +0x18 от конца ссылок.</summary>
    /// <returns>Диапазон, тег и точки движения.</returns>
    /// <exception cref="FwxFormatException">Элемент адаптера не сходится.</exception>
    private static FieldMovement Adapted(FwxDocument doc, VarReference link)
    {
        var table = doc.FindTable(link.Table)!;
        var item = ElementLink.Item(doc, table, link.Index);
        var b = doc.Binary;
        var (end, links) = ReferenceHeader.Read(b, item, table.Name);
        if (links.Count == 0 || end + BodySize > item.Length || b.D4(item.Offset + end) != 2)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: движение по диапазону не разобрано");
        }
        var p = item.Offset + end;
        return new FieldMovement(false, Tag(doc, links[0], table.Name, item.Offset + 0x08), null, b.F64(p + 0x08), b.F64(p + 0x10),
            (short)b.D2(p + 0x18), (short)b.D2(p + 0x1a), (short)b.D2(p + 0x1c), (short)b.D2(p + 0x1e));
    }

    /// <summary>Имя тега; неизвестное имя — знак вопроса, повреждённая ссылка — ошибка формата.</summary>
    /// <returns>Имя тега.</returns>
    /// <exception cref="FwxFormatException">Таблица тега отсутствует или элемент вне таблицы.</exception>
    private static string Tag(FwxDocument doc, VarReference link, string source, long offset)
    {
        var table = AnimationLinkReader.Table(doc, link, source, offset);
        if (table.Name is "VAR" or "VARLIST")
        {
            ElementLink.Item(doc, table, link.Index);
        }
        return IoFieldTagReader.Read(doc, [link]).Name ?? "?";
    }
}
