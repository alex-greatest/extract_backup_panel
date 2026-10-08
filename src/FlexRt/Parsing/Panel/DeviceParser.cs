using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Разбор DEVICE_OMSP: сетевые параметры самой панели — IP и маска.</summary>
public static class DeviceParser
{
    /// <summary>
    /// Смещение IP панели в элементе. Наблюдение: <c>01 00 00 00 00 00 00 00 00 00 03 00</c>, затем
    /// IP и маска (TIA V21 / TP700 — 192.168.0.2, сверено с проектом; TIA V17 / TP1500 — 192.168.1.3
    /// в одной подсети с ПЛК 192.168.1.1).
    /// </summary>
    private const int IpOffset = 0x0c;

    /// <summary>Смещение маски подсети: сразу за IP.</summary>
    private const int MaskOffset = 0x10;

    /// <summary>Начало элемента, наблюдавшееся на всех файлах: 10 байт нулей после <c>01</c>, затем <c>03 00</c>.</summary>
    private static readonly byte[] Prefix = [0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x03, 0x00];

    /// <summary>
    /// Прочитать таблицу DEVICE_OMSP и добавить панель в <see cref="FwxDocument.Devices"/>. Нет
    /// таблицы в TOC — ничего не делает. Элемент другой раскладки (короче IP и маски или с другим
    /// началом) пропускается без предупреждения: на листе «Сводка» IP панели будет «неизвестно».
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("DEVICE_OMSP");
        if (table is null)
        {
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            if (ReadRecord(doc.Binary, item) is { } device)
            {
                doc.Devices.Add(device);
            }
        }
    }

    /// <summary>Один элемент: <see cref="Prefix"/>, IP на <see cref="IpOffset"/> и маска на <see cref="MaskOffset"/>, первая часть адреса первым байтом.</summary>
    /// <returns>Сетевые параметры панели или <c>null</c>, если раскладка другая.</returns>
    private static HmiDevice? ReadRecord(FwxBinary b, TableItem item)
    {
        if (item.Length < MaskOffset + 4 || !b.Span(item.Offset, Prefix.Length).SequenceEqual(Prefix))
        {
            return null;
        }
        return new HmiDevice(b.D4BigEndian(item.Offset + IpOffset), b.D4BigEndian(item.Offset + MaskOffset));
    }
}
