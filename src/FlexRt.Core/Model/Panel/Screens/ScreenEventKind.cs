namespace FlexRt.Model.Panel.Screens;

/// <summary>Событие экрана в TIA (вкладка Events экрана): Loaded или Cleared.</summary>
public enum ScreenEventKind
{
    /// <summary>Loaded: запись SCREEN ссылается на FUNC_LIST (сверено с TIA на шагах <c>data/real/steps-events/</c>).</summary>
    Loaded,

    /// <summary>Cleared: запись SCREEN ссылается на ADAPTDEMULTIPLE (сверено с TIA на шагах <c>data/real/steps-events/</c>).</summary>
    Cleared
}
