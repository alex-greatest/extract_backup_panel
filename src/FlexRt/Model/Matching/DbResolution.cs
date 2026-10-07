namespace FlexRt.Model.Matching;

/// <summary>Результат разрешения символьного пути в DB.</summary>
/// <param name="Lookup">Найден ли член; <see cref="PlcLookup.Unknown"/> — DB подходящего номера не читается.</param>
/// <param name="Path">Путь как в TIA (<c>DB_X.Axis.Button[3]</c>); пустая строка, если не найден.</param>
/// <param name="Type">Тип конечного члена с учётом индексов; пустая строка, если не найден.</param>
/// <param name="Comment">Комментарий конечного члена (первый непустой текст); пустая строка, если комментария нет.</param>
public sealed record DbResolution(PlcLookup Lookup, string Path, string Type, string Comment);
