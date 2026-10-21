using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>
/// Проверка раскладки каждой таблицы TOC, в том числе таблиц без своего парсера: заголовок (0x34 байта), каталог
/// смещений и элементы (<see cref="FwxReader.Items"/>) лежат внутри файла, длины элементов не отрицательные. Ту же
/// проверку раньше делала hex-выгрузка таблиц консоли.
/// </summary>
public static class TableLayoutCheck
{
    /// <summary>
    /// Проверить все таблицы TOC по порядку. Таблица не той раскладки — сообщение в <see cref="FwxDocument.Warnings"/>
    /// (то же сообщение, что уже дал парсер таблицы, второй раз не добавляется), остальные таблицы проверяются.
    /// </summary>
    public static void Run(FwxDocument doc)
    {
        foreach (var table in doc.Toc)
        {
            try
            {
                Check(doc.Binary, table);
            }
            catch (FwxFormatException e) when (!doc.Warnings.Contains(e.Message))
            {
                doc.Warnings.Add(e.Message);
            }
            catch (FwxFormatException)
            {
                // то же сообщение уже записал парсер таблицы
            }
        }
    }

    /// <summary>Проверить одну таблицу: заголовок, затем каждый элемент.</summary>
    /// <exception cref="FwxFormatException">Заголовок или элемент выходит за пределы файла, длина элемента отрицательная.</exception>
    private static void Check(FwxBinary b, TocEntry table)
    {
        b.Section = table.Name;
        b.Check(table.Offset, FwxReader.TableHeaderSize);
        foreach (var item in FwxReader.Items(b, table))
        {
            b.Check(item.Offset, item.Length);
        }
    }
}
