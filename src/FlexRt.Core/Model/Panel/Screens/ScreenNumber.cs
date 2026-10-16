namespace FlexRt.Model.Panel.Screens;

/// <summary>Номер экрана (Screen number в TIA) и запись SCREEN, к которой он относится: пара из ADAPTVALUECHANG.</summary>
/// <param name="Number">Номер экрана, как в свойствах экрана в TIA.</param>
/// <param name="ScreenIndex">Номер записи в таблице SCREEN, с нуля (<see cref="Screen.Index"/>).</param>
public readonly record struct ScreenNumber(int Number, int ScreenIndex);
