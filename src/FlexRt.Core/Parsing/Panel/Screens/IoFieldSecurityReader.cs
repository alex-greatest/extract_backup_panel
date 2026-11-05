using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Authorization I/O field: номер права за ссылками SECURITY_MONITO, шаги 60–62, V21.</summary>
internal static class IoFieldSecurityReader
{
    /// <summary>Таблица требуемого права объекта.</summary>
    private const string Table = "SECURITY_MONITO";

    /// <summary>Прочитать номер права с нуля по ссылке поля.</summary>
    /// <returns>Номер права; ссылки нет — null.</returns>
    /// <exception cref="FwxFormatException">Ссылка или элемент права не сходятся.</exception>
    public static long? Read(FwxDocument doc, IReadOnlyList<VarReference> links)
    {
        foreach (var link in links)
        {
            if (doc.FindTable(link.Table) is not { Name: Table } table)
            {
                continue;
            }
            var item = ElementLink.Item(doc, table, link.Index);
            var (end, _) = ReferenceHeader.Read(doc.Binary, item, table.Name);
            if (doc.Binary.D4(item.Offset) != 1 || end + 4 > item.Length)
            {
                throw new FwxFormatException(table.Name, item.Offset,
                    $"запись {item.Index + 1}: номер права Authorization не прочитан");
            }
            return doc.Binary.D4(item.Offset + end);
        }
        return null;
    }
}
