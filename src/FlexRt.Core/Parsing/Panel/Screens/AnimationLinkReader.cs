using FlexRt.Binary;
using FlexRt.Model.Panel;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>Таблица цели существующей ссылки анимации; отсутствующая таблица не означает отсутствие связи.</summary>
internal static class AnimationLinkReader
{
    /// <summary>Найти таблицу цели; смещение указывает на повреждённую ссылку в таблице источника.</summary>
    /// <returns>Таблица цели.</returns>
    /// <exception cref="FwxFormatException">Id таблицы отсутствует в TOC.</exception>
    public static TocEntry Table(FwxDocument doc, VarReference link, string source, long offset) =>
        doc.FindTable(link.Table)
        ?? throw new FwxFormatException(source, offset, $"таблица цели ссылки Id 0x{link.Table:X} отсутствует в TOC");
}
