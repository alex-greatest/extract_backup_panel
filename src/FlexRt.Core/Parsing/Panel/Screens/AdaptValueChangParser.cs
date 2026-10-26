using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Разбор ADAPTVALUECHANG: номера экранов — элемент со ссылкой на GUI, пары (номер экрана, запись SCREEN).</summary>
public static class AdaptValueChangParser
{
    /// <summary>
    /// Смещение u16 числа пар K от конца ссылок; перед ним u16 (наблюдение: всегда 0x12, не разобрано).
    /// </summary>
    private const int PairCountOffset = 2;

    /// <summary>Начало пар от конца ссылок: за u16, u16 K и u32 (наблюдение: всегда 2, не разобрано).</summary>
    private const int PairsOffset = 8;

    /// <summary>Размер пары: u16 номер экрана, u16 номер записи SCREEN.</summary>
    private const int PairSize = 4;

    /// <summary>
    /// Прочитать таблицу ADAPTVALUECHANG и добавить номера экранов в <see cref="FwxDocument.ScreenNumbers"/>
    /// из элементов, ссылающихся на таблицу GUI (в файлах такой элемент один, первый). У остальных элементов
    /// читается только начало со ссылками. Нет таблицы ADAPTVALUECHANG или GUI — номеров нет.
    /// <see cref="FwxDocument.ScreenNumbersComplete"/> становится <c>true</c>, только если таблицы нет или она
    /// прочитана целиком.
    /// </summary>
    /// <exception cref="FwxFormatException">Элемент выходит за пределы таблицы или не сходится с раскладкой.</exception>
    public static void Parse(FwxDocument doc)
    {
        var table = doc.FindTable("ADAPTVALUECHANG");
        var gui = doc.FindTable("GUI");
        if (table is null || gui is null)
        {
            doc.ScreenNumbersComplete = true;
            return;
        }

        foreach (var item in FwxReader.CheckedItems(doc.Binary, table))
        {
            ReadRecord(doc, item, gui.Id);
        }
        doc.ScreenNumbersComplete = true;
    }

    /// <summary>
    /// Элемент (сверено с номерами экранов в TIA на шаговых копиях Project1, V21): начало со ссылками
    /// (<see cref="ReferenceHeader"/>); если среди ссылок есть ссылка на GUI — за ними u16, u16 K, u32 и K пар
    /// (u16 номер экрана, u16 номер записи SCREEN). Каждая пара сразу добавляется в документ.
    /// </summary>
    /// <exception cref="FwxFormatException">Начало элемента не сходится или пары не помещаются в элемент.</exception>
    private static void ReadRecord(FwxDocument doc, TableItem item, int guiId)
    {
        var b = doc.Binary;
        var (end, links) = ReferenceHeader.Read(b, item);
        if (links.All(r => r.Table != guiId))
        {
            return;
        }
        var p = item.Offset;
        if (end + PairsOffset > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {item.Index + 1}: число пар не помещается в {item.Length} байт");
        }
        var count = b.D2(p + end + PairCountOffset);
        if (end + PairsOffset + count * (long)PairSize > item.Length)
        {
            throw new FwxFormatException(b.Section, p, $"запись {item.Index + 1}: {count} пар не помещаются в {item.Length} байт");
        }
        var pairs = p + end + PairsOffset;
        for (var i = 0; i < count; i++)
        {
            var pair = pairs + i * PairSize;
            doc.ScreenNumbers.Add(new ScreenNumber(b.D2(pair), b.D2(pair + 2)));
        }
    }
}
