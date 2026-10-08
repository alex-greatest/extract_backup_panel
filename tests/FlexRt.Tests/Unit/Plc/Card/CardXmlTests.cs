using FlexRt.Binary;
using FlexRt.Model.Plc.Card;
using FlexRt.Parsing.Plc.Card;
using Xunit;

namespace FlexRt.Tests.Unit.Plc.Card;

/// <summary>XML потоков карты ПЛК (<see cref="CardXml"/>): разбор и комментарии.</summary>
public sealed class CardXmlTests
{
    /// <summary>Файл потока в сообщениях.</summary>
    private const string StreamFile = @"000009FA\1\00000040";

    /// <summary>Пустой поток комментариев (у блока без комментариев распаковывается в 0 байт) — комментариев нет, не ошибка.</summary>
    [Fact]
    public void AddComments_EmptyStream_NoComments()
    {
        var comments = new Dictionary<string, string>();

        CardXml.AddComments(new CardStream(StreamFile, 0, 0, CardStreamKind.BlockComments, ""), "Path", comments);

        Assert.Empty(comments);
    }

    /// <summary>
    /// Комментарий — первый непустой <c>DictEntry</c>; комментарий без текста не попадает; при
    /// повторном ключе остаётся первый; BOM в начале текста не мешает.
    /// </summary>
    [Fact]
    public void AddComments_FirstNonEmptyEntryAndFirstKey()
    {
        const string text = "﻿<InterfaceLineComments><Part Kind=\"Comments\">"
            + "<Comment Path=\"51:60\"><DictEntry Language=\"en-US\"></DictEntry><DictEntry Language=\"ru-RU\">Дверь</DictEntry></Comment>"
            + "<Comment Path=\"52\"><DictEntry Language=\"ru-RU\"></DictEntry></Comment>"
            + "<Comment Path=\"51:60\"><DictEntry Language=\"ru-RU\">повтор</DictEntry></Comment>"
            + "</Part></InterfaceLineComments>";
        var comments = new Dictionary<string, string>();

        CardXml.AddComments(new CardStream(StreamFile, 0x112c, 0, CardStreamKind.BlockComments, text), "Path", comments);

        Assert.Equal(new Dictionary<string, string> { ["51:60"] = "Дверь" }, comments);
    }

    /// <summary>Битый XML — ошибка формата с файлом и смещением потока.</summary>
    [Fact]
    public void Parse_BrokenXml_ThrowsWithFileAndOffset()
    {
        var error = Assert.Throws<FwxFormatException>(() =>
            CardXml.Parse(new CardStream(StreamFile, 0x14c6, 0, CardStreamKind.BlockInterface, "<BlockInterface>")));

        Assert.Equal(StreamFile, error.Section);
        Assert.Equal(0x14c6, error.Offset);
    }
}
