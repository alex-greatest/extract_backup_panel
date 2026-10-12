namespace FlexRt.Model.Plc;

/// <summary>Область памяти ПЛК, к которой привязан тег.</summary>
public enum PlcArea
{
    /// <summary>Входы, <c>%I</c>.</summary>
    Input,
    /// <summary>Выходы, <c>%Q</c>.</summary>
    Output,
    /// <summary>Меркеры, <c>%M</c>.</summary>
    Memory,
    /// <summary>Счётчики, <c>%C</c>.</summary>
    Counter,
    /// <summary>Таймеры, <c>%T</c>.</summary>
    Timer,
    /// <summary>Блок данных, <c>%DB</c>.</summary>
    DataBlock
}
