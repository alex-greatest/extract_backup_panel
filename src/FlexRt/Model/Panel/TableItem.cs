namespace FlexRt.Model.Panel;

/// <summary>
/// Элемент таблицы: индекс, смещение и длина — до следующего элемента,
/// у последнего — до конца таблицы.
/// </summary>
/// <param name="Index">Номер элемента в таблице, с нуля.</param>
/// <param name="Offset">Смещение элемента от начала файла.</param>
/// <param name="Length">Длина элемента в байтах.</param>
public readonly record struct TableItem(int Index, long Offset, long Length);
