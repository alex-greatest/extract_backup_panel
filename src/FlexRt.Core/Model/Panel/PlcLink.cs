using FlexRt.Model.Plc;

namespace FlexRt.Model.Panel;

/// <summary>Связь PLC-тега с ПЛК: элемент таблицы DATALINK_READWR.</summary>
/// <param name="Index">Номер элемента в таблице, с нуля; на него ссылается запись VAR.</param>
/// <param name="Connection">Номер соединения, с нуля: индекс записи CONNECTION_OMSP.</param>
/// <param name="CycleMs">Цикл опроса в миллисекундах.</param>
/// <param name="Absolute">Абсолютный доступ; иначе символьный.</param>
/// <param name="PlcTypeCode">Код типа ПЛК.</param>
/// <param name="Area">Область памяти; <c>null</c>, если код области неизвестен.</param>
/// <param name="DbNumber">Номер DB у области <see cref="PlcArea.DataBlock"/>, иначе 0.</param>
/// <param name="Bit">Номер бита; только при абсолютном доступе.</param>
/// <param name="ByteOffset">Номер байта, у таймера и счётчика — номер; только при абсолютном доступе.</param>
/// <param name="SymbolId">ID символа I/Q/M/C/T — первое слово пути без вида в старшем полубайте; только при символьном доступе к ним.</param>
/// <param name="Hash">Хэш символа (назначение не установлено); только при символьном доступе.</param>
/// <param name="Path">Путь символа: u32 на каждый уровень (у DB — члены; у I/Q/M/C/T — ID тега, у тега пользовательского типа за ним LID членов).</param>
/// <param name="BitSize">Размер значения ПЛК в битах (у массива и строки — всего).</param>
/// <param name="Elements">Число элементов: 1 у скаляра, у массива и строки — их длина.</param>
/// <param name="Offset">Смещение элемента от начала файла.</param>
/// <param name="Decoded">
/// Адрес или символ разобран. <c>false</c> — раскладка элемента незнакома: область, ID и адрес
/// неизвестны, общие поля прочитаны.
/// </param>
public sealed record PlcLink(
    int Index, int Connection, long CycleMs, bool Absolute, int PlcTypeCode, PlcArea? Area, int DbNumber,
    int Bit, long ByteOffset, long SymbolId, long Hash, IReadOnlyList<long> Path, int BitSize, int Elements,
    long Offset, bool Decoded);
