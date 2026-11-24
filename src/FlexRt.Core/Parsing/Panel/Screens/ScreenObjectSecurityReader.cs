using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Чтение свойств Security у объектов экрана.</summary>
internal static class ScreenObjectSecurityReader
{
    /// <summary>Бит отключения Allow operator control в стиле окна I/O field и кнопки (TP700 Comfort, V21).</summary>
    private const long DisabledStyle = 0x08000000;

    /// <summary>Таблица требуемого права объекта.</summary>
    private const string Table = "SECURITY_MONITO";

    /// <summary>Проверить бит отключения во флагах объекта за ссылками.</summary>
    /// <returns>Разрешено ли управление оператору.</returns>
    public static bool AllowsOperatorControl(long windowStyle) => (windowStyle & DisabledStyle) == 0;

    /// <summary>Прочитать номер права с нуля по ссылке объекта на SECURITY_MONITO.</summary>
    /// <returns>Номер права; ссылки нет — null.</returns>
    /// <exception cref="FwxFormatException">Ссылка или элемент права не сходятся.</exception>
    public static long? ReadAuthorization(FwxDocument doc, IReadOnlyList<VarReference> links)
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
