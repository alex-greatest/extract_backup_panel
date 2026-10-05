namespace FlexRt.Model;

/// <summary>
/// Заголовок FWX. Пары «Entries/Offset» — число элементов и смещение служебного массива.
/// Назначение массивов init, info и metainfo не установлено, имена условные.
/// </summary>
/// <param name="Start1">Сигнатура, всегда 0xbeef.</param>
/// <param name="Start2">Всегда 0xc.</param>
/// <param name="Start3">Всегда 0x0 (TIA V17).</param>
/// <param name="Start4">Всегда 0x1100 (TIA V17).</param>
/// <param name="Start5">Всегда 0x1.</param>
/// <param name="Start6">Всегда 0x1.</param>
/// <param name="PostTablesPadding1">Гипотеза: размер буфера значений тегов, лежащего сразу за таблицами.</param>
/// <param name="PostTablesPadding2">Всегда равно <see cref="PostTablesPadding1"/>.</param>
/// <param name="TablesEnd">Конец таблиц: по нему считается размер последней таблицы.</param>
/// <param name="TocEntries">Число записей Table Of Contents.</param>
/// <param name="TocOffset">Смещение Table Of Contents.</param>
/// <param name="InitEntries">Число записей массива init.</param>
/// <param name="InitOffset">Смещение массива init.</param>
/// <param name="InfoEntries">Число записей массива info.</param>
/// <param name="InfoOffset">Смещение массива info.</param>
/// <param name="MetainfoEntries">Число записей массива metainfo.</param>
/// <param name="MetainfoOffset">Смещение массива metainfo.</param>
/// <param name="LangEntries">Число языков (наблюдение: 1 при одном языке).</param>
/// <param name="LangOffset">Смещение списка языков: LCID по 16 бит (наблюдение: 0x409).</param>
public sealed record FwxHeader(
    int Start1, int Start2, int Start3, int Start4, int Start5, int Start6,
    long PostTablesPadding1, long PostTablesPadding2,
    long TablesEnd,
    long TocEntries, long TocOffset,
    long InitEntries, long InitOffset,
    long InfoEntries, long InfoOffset,
    long MetainfoEntries, long MetainfoOffset,
    long LangEntries, long LangOffset);
