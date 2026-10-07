using FlexRt.Model.Panel;
using FlexRt.Model.Plc;

namespace FlexRt.Tests.Unit;

/// <summary>
/// Связи <see cref="PlcLink"/> для юнит-тестов: только поля, которые читают проверяемые
/// функции; остальные нули. Значения полей в тестах взяты из разбора
/// <c>samples/plc/pdata.fwc</c> (TIA V21 / S7-1500) или из документации формата.
/// </summary>
public static class PlcLinks
{
    /// <summary>Цикл опроса по умолчанию, мс: у большинства тегов <c>samples/plc</c> — 1 s.</summary>
    private const long DefaultCycleMs = 1000;

    /// <summary>Связь с абсолютным доступом.</summary>
    /// <param name="area">Область памяти.</param>
    /// <param name="byteOffset">Номер байта, у таймера и счётчика — номер.</param>
    /// <param name="bit">Номер бита.</param>
    /// <param name="bitSize">Размер значения ПЛК в битах.</param>
    /// <param name="elements">Число элементов.</param>
    /// <param name="dbNumber">Номер DB.</param>
    /// <param name="plcTypeCode">Код типа ПЛК.</param>
    /// <returns>Связь абсолютного доступа через соединение 0 с циклом 1 s.</returns>
    public static PlcLink Absolute(PlcArea? area, long byteOffset, int bit, int bitSize, int elements = 1, int dbNumber = 0, int plcTypeCode = 0) =>
        new(0, 0, DefaultCycleMs, true, plcTypeCode, area, dbNumber, bit, byteOffset, 0, 0, [], bitSize, elements, 0, true);

    /// <summary>Связь с символьным доступом.</summary>
    /// <param name="area">Область памяти.</param>
    /// <param name="plcTypeCode">Код типа ПЛК.</param>
    /// <param name="cycleMs">Цикл опроса, мс.</param>
    /// <param name="elements">Число элементов.</param>
    /// <param name="connection">Номер соединения.</param>
    /// <returns>Связь символьного доступа.</returns>
    public static PlcLink Symbolic(PlcArea? area, int plcTypeCode, long cycleMs = DefaultCycleMs, int elements = 1, int connection = 0) =>
        new(0, connection, cycleMs, false, plcTypeCode, area, 0, 0, 0, 0, 0, [], 0, elements, 0, true);
}
