namespace FlexRt.Model.Panel.Screens;

/// <summary>Режим I/O field, как Type → Mode в TIA.</summary>
public enum IoFieldMode
{
    /// <summary>Input: элемент VIEW_IN, слово режима 0.</summary>
    Input,

    /// <summary>Input/output: элемент VIEW_IN, слово режима <c>0xFFFF</c>.</summary>
    InputOutput,

    /// <summary>Output: элемент VIEW_OUT.</summary>
    Output
}
