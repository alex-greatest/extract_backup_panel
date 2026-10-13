namespace FlexRt.Model.Panel.Alarms;

/// <summary>Бит тега, привязанный к дискретному аларму.</summary>
/// <param name="AlarmId">ID аларма (<see cref="DiscreteAlarm.Id"/>).</param>
/// <param name="Bit">Номер бита в теге, как Trigger bit в TIA (у массива — сквозной по всем элементам).</param>
public readonly record struct AlarmBit(long AlarmId, int Bit);
