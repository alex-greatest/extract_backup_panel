namespace FlexRt.Parsing.Panel.Functions;

/// <summary>Другая функция TIA с теми же таблицей и подкодом (<see cref="SystemFunction.FirstReference"/>).</summary>
/// <param name="Names">Имена функции в TIA.</param>
/// <param name="Parameters">Имена параметров в TIA по порядку параметров в элементе.</param>
internal sealed record SystemFunctionVariant(IReadOnlyList<string> Names, IReadOnlyList<string> Parameters);
