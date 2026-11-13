using FlexRt.Binary;
using FlexRt.Export.ScreenEvents;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Functions;
using FlexRt.Model.Panel.Screens;
using Xunit;

namespace FlexRt.Tests.Unit.Export;

/// <summary>Имя целевого экрана в параметрах ActivateScreen; номер объекта и динамический выбор по тегу не подменяются.</summary>
public sealed class ScreenEventTextsTests
{
    /// <summary>Номер экрана сопоставляется с записью SCREEN, а такой же номер второго параметра остаётся числом.</summary>
    [Theory]
    [InlineData(FunctionArgumentKind.Integer)]
    [InlineData(FunctionArgumentKind.UInt32)]
    public void ActivateScreen_NumberBecomesName(FunctionArgumentKind kind)
    {
        var doc = Document();
        var call = Call("ActivateScreen", [new FunctionArgument(kind, Number: 42), new FunctionArgument(kind, Number: 42)]);

        var function = View(doc, call);

        Assert.Equal("ActivateScreen", function.Name);
        Assert.Equal(["Target", "42"], function.Parameters.Select(p => p.Value));
    }

    /// <summary>Нет пары номера или самой записи SCREEN — в параметре остаётся прочитанный номер, без выдуманного имени.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActivateScreen_UnresolvedNumberStaysNumeric(bool missingNumber)
    {
        var doc = Document();
        if (missingNumber)
        {
            doc.ScreenNumbers.Clear();
        }
        else
        {
            doc.Screens.RemoveAll(s => s.Index == 123);
        }

        var function = View(doc, Call("ActivateScreen", [new FunctionArgument(FunctionArgumentKind.Integer, Number: 42)]));

        Assert.Equal("42", Assert.Single(function.Parameters).Value);
    }

    /// <summary>ActivateScreenByNumber показывает тег: его значение во время работы панели заранее не известно.</summary>
    [Fact]
    public void ActivateScreenByNumber_KeepsTagName()
    {
        var doc = Document();
        doc.Toc.Add(new TocEntry { Id = 1, Name = "VAR" });
        doc.Tags.Add(new HmiTag(1, "ScreenSelection", 0, 1, 0, 0, null, null));
        var call = Call("ActivateScreenByNumber",
            [new FunctionArgument(FunctionArgumentKind.Tag, Reference: new VarReference(1, 0)), new FunctionArgument(FunctionArgumentKind.Integer, Number: 42)]);

        var function = View(doc, call);

        Assert.Equal(["ScreenSelection", "42"], function.Parameters.Select(p => p.Value));
    }

    /// <summary>Совпавшее число у другой функции остаётся числом, даже если имя параметра — Screen name.</summary>
    [Fact]
    public void OtherFunction_NumberStaysNumeric()
    {
        var function = View(Document(), Call("SetTag", [new FunctionArgument(FunctionArgumentKind.Integer, Number: 42)]));

        Assert.Equal("42", Assert.Single(function.Parameters).Value);
    }

    /// <summary>Модель документа для форматирования: номер 42 относится к записи SCREEN 123, их индексы намеренно различаются.</summary>
    /// <returns>Данные модели, без создания двоичного файла.</returns>
    private static FwxDocument Document()
    {
        var doc = new FwxDocument
        {
            Binary = new FwxBinary([]),
            Header = new FwxHeader(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
        };
        doc.Screens.Add(new Screen(10, false, "Source", 800, 480, [], null, []));
        doc.Screens.Add(new Screen(123, false, "Target", 800, 480, [], null, []));
        doc.ScreenNumbers.Add(new ScreenNumber(42, 123));
        return doc;
    }

    /// <summary>Вызов с именами параметров ActivateScreen; форматирование должно учитывать и имя функции.</summary>
    /// <returns>Вызов функции из модели.</returns>
    private static FunctionCall Call(string name, IReadOnlyList<FunctionArgument> arguments) =>
        new(new VarReference(2, 0), "FUNC_SETSYSVAR", 2, [name], ["Screen name", "Object number"], arguments);

    /// <summary>Показать вызов в Loaded исходного экрана, как его получает окно.</summary>
    /// <returns>Единственная функция события.</returns>
    private static FunctionView View(FwxDocument doc, FunctionCall call)
    {
        doc.ScreenEvents.Add(new ScreenEvent(10, ScreenEventKind.Loaded, [call]));
        var loaded = new ScreenEventTexts(doc).Of(doc.Screens.Single(s => s.Index == 10))[0];
        return Assert.Single(loaded.Functions);
    }
}
