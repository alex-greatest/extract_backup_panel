namespace FlexRt.Model.Panel.Alarms;

/// <summary>Предел аналогового аларма: элемент таблицы MSG_ANALOG_OBSE, по одному на аларм.</summary>
/// <param name="Index">Номер элемента в таблице, с нуля; на него ссылается запись VAR тега срабатывания.</param>
/// <param name="AlarmId">ID аларма (<see cref="AnalogAlarm.Id"/>).</param>
/// <param name="Mode">Байт Limit mode как в файле: 1 — Higher, 0 — Lower.</param>
/// <param name="ValueKind">
/// Байт вида значения предела как в файле: 1 — целое со знаком, 2 — float32, 3 — float64
/// (установлено на Project1); 0 — целое без знака или предел-тег (гипотеза).
/// </param>
/// <param name="Value">Значение предела — 4 байта как u32, у float64 — 8 байт как u64; у предела-тега 0.</param>
/// <param name="LimitTag">Номер записи VAR тега предела, с нуля; у предела-константы <c>null</c>.</param>
public sealed record AnalogAlarmLimit(int Index, long AlarmId, int Mode, int ValueKind, long Value, int? LimitTag);
