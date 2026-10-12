namespace FlexRt.Model.Panel;

/// <summary>Ссылка записи VAR на элемент другой таблицы.</summary>
/// <param name="Table">Id таблицы из TOC.</param>
/// <param name="Index">Номер элемента в таблице, с нуля.</param>
public readonly record struct VarReference(int Table, int Index);
