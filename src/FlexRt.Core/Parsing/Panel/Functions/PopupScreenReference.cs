using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;

namespace FlexRt.Parsing.Panel.Functions;

/// <summary>Имя pop-up по цепочке FUNC_SHOWPOPUPS → SCREEN_SYS → служебная запись SCREEN → экран (шаг 88, V21).</summary>
internal static class PopupScreenReference
{
    /// <summary>Смещение u16 количества объектов SCREEN_SYS от конца ссылок: стиль, рамка и служебные поля.</summary>
    private const int CountOffset = 0x12;

    /// <summary>Смещение первой пары объекта SCREEN_SYS от конца ссылок: u16 индекс, u16 Id таблицы.</summary>
    private const int PairOffset = 0x14;

    /// <summary>Размер пары объекта.</summary>
    private const int PairSize = 4;

    /// <summary>Разрешить имя pop-up; неизвестную структуру обёртки оставить исходным числом.</summary>
    /// <returns>Аргумент с именем SCREEN либо исходный параметр.</returns>
    /// <exception cref="Binary.FwxFormatException">Ссылка SCREEN_SYS ведёт за пределы таблицы.</exception>
    public static FunctionArgument Resolve(FwxDocument doc, FunctionArgument argument)
    {
        if (argument.Kind != FunctionArgumentKind.UInt32 || argument.Number > int.MaxValue
            || doc.FindTable("SCREEN_SYS") is not { } system || doc.FindTable("SCREEN") is not { } screens)
        {
            return argument;
        }
        var item = ElementLink.Item(doc, system, (int)argument.Number);
        var b = doc.Binary;
        if (NamedElement.TryBodyOffset(b, item) is not { } body || body + PairOffset + PairSize > item.Length
            || b.D2(item.Offset + body + CountOffset) != 1
            || b.D2(item.Offset + body + PairOffset + 2) != screens.Id)
        {
            return argument;
        }
        var index = b.D2(item.Offset + body + PairOffset);
        var screen = doc.Screens.FirstOrDefault(s => s.Index == index);
        if (screen is { IsService: true, Objects.Count: 1 } && screen.Objects[0].Table == screens.Id)
        {
            screen = doc.Screens.FirstOrDefault(s => s.Index == screen.Objects[0].Index);
        }
        return screen is { IsService: false, Name.Length: > 0 }
            ? new FunctionArgument(FunctionArgumentKind.ScreenObject, Text: screen.Name, Reference: new VarReference(screens.Id, screen.Index))
            : argument;
    }
}
