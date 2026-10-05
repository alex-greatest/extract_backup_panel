namespace FlexRt.Model;

/// <summary>
/// Заголовок FWX. Пары «Entries/Offset» — число элементов и смещение служебного массива.
/// Назначение массивов init, info и metainfo не установлено, имена условные.
/// </summary>
/// <param name="Start1">Сигнатура, всегда 0xbeef.</param>
/// <param name="Start2">Всегда 0xc.</param>
/// <param name="Start3">Всегда 0x1.</param>
/// <param name="Start4">Всегда 0x703.</param>
/// <param name="Start5">Всегда 0x1.</param>
/// <param name="Start6">Всегда 0x1.</param>
/// <param name="PostTablesPadding1">Размер выравнивания в конце файла (предположительно).</param>
/// <param name="PostTablesPadding2">Размер выравнивания в конце файла (предположительно).</param>
/// <param name="TablesEnd">Конец таблиц: по нему считается размер последней таблицы.</param>
/// <param name="TocEntries">Число записей Table Of Contents.</param>
/// <param name="TocOffset">Смещение Table Of Contents.</param>
/// <param name="InitEntries">Число записей массива init.</param>
/// <param name="InitOffset">Смещение массива init.</param>
/// <param name="InfoEntries">Число записей массива info.</param>
/// <param name="InfoOffset">Смещение массива info.</param>
/// <param name="MetainfoEntries">Число записей массива metainfo.</param>
/// <param name="MetainfoOffset">Смещение массива metainfo.</param>
/// <param name="LangEntries">Число языков.</param>
/// <param name="LangOffset">Смещение списка языков.</param>
public sealed record FwxHeader(
    int Start1, int Start2, int Start3, int Start4, int Start5, int Start6,
    long PostTablesPadding1, long PostTablesPadding2,
    long TablesEnd,
    long TocEntries, long TocOffset,
    long InitEntries, long InitOffset,
    long InfoEntries, long InfoOffset,
    long MetainfoEntries, long MetainfoOffset,
    long LangEntries, long LangOffset);
