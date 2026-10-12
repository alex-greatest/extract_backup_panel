namespace FlexRt.Model.Panel;

/// <summary>Поле вывода в тексте дискретного аларма: значение тега или текст из текстового списка.</summary>
/// <param name="TextList">Поле текстового списка (<c>&lt;Text list, …&gt;</c>); иначе поле тега (<c>&lt;Tag : 6, …&gt;</c>).</param>
/// <param name="Length">Длина поля тега в символах; у поля текстового списка — номер списка в панели (гипотеза).</param>
/// <param name="SourceTable">Id таблицы, где лежит связь тега поля с ПЛК (DATALINK); 0 — ссылки нет.</param>
/// <param name="SourceIndex">Номер элемента в таблице <paramref name="SourceTable"/>, с нуля.</param>
public sealed record AlarmField(bool TextList, int Length, int SourceTable, int SourceIndex);
