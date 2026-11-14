namespace FlexRt.Export.ScreenObjects;

/// <summary>Названия скоростей мигания Appearance, сверенные с обратным XML TIA V21.</summary>
public static class AppearanceAnimationTexts
{
    /// <summary>Код SYMB: 0 No, 1 Slow, 2 Medium, 3 Fast; неизвестный код не подменяется выключенным миганием.</summary>
    /// <returns>Название из TIA либо неизвестный код.</returns>
    public static string Flashing(int code) => code switch
    {
        0 => "No",
        1 => "Slow",
        2 => "Medium",
        3 => "Fast",
        _ => $"? (0x{code:X2})"
    };
}
