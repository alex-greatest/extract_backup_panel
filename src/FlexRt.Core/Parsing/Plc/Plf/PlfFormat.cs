namespace FlexRt.Parsing.Plc.Plf;

/// <summary>Константы формата PEData.plf, общие для нескольких разборщиков.</summary>
internal static class PlfFormat
{
    /// <summary>Имя секции в сообщениях об ошибках.</summary>
    public const string Section = "PEData.plf";

    /// <summary>Класс объекта ПЛК.</summary>
    public const long PlcClass = 0x0010101c;

    /// <summary>Тип связи «объект → ПЛК» (у тегов, DB и сетевых интерфейсов).</summary>
    public const long PlcRelation = 0x00012003;

    /// <summary>Наименьшее смещение за заголовком объекта: слоты смещений и связи лежат не ближе.</summary>
    public const long MinSlot = 0x60;
}
