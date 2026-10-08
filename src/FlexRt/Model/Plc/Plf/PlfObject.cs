namespace FlexRt.Model.Plc.Plf;

/// <summary>Актуальная версия объекта в PEData.plf: данные кадра после поля длины.</summary>
/// <param name="Class">Класс объекта (<c>0x0002105c</c> — тег ПЛК).</param>
/// <param name="Id">ID объекта внутри проекта.</param>
/// <param name="Offset">Смещение данных объекта от начала файла.</param>
/// <param name="Length">Длина данных объекта до байта <c>ff</c> и контрольной суммы.</param>
internal readonly record struct PlfObject(long Class, long Id, long Offset, long Length);
