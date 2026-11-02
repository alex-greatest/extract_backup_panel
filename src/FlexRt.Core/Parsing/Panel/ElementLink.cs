using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel;

/// <summary>Элемент другой таблицы по ссылке (u16 Id таблицы, u16 номер элемента): элемент по номеру с проверкой номера.</summary>
internal static class ElementLink
{
    /// <summary>
    /// Элемент таблицы по номеру из ссылки: номер проверяется по <see cref="TocEntry.Entries"/>, элемент —
    /// <see cref="FwxReader.CheckedItem"/>.
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="table">Таблица элемента.</param>
    /// <param name="index">Номер элемента из ссылки, с нуля.</param>
    /// <param name="owner">Чья ссылка — для сообщения (<c>экран Screen_2</c>); <c>null</c> — «ссылка».</param>
    /// <returns>Номер, начало и длина элемента.</returns>
    /// <exception cref="FwxFormatException">
    /// Ссылка ведёт за последний элемент, запись каталога смещений выходит за файл или даёт отрицательную длину,
    /// элемент выходит за пределы таблицы.
    /// </exception>
    public static TableItem Item(FwxDocument doc, TocEntry table, int index, string? owner = null) =>
        index < table.Entries
            ? FwxReader.CheckedItem(doc.Binary, table, index)
            : throw new FwxFormatException(
                table.Name,
                table.Offset,
                $"{(owner is null ? "ссылка на элемент" : $"{owner} ссылается на элемент")} {index + 1}, а элементов {table.Entries}");
}
