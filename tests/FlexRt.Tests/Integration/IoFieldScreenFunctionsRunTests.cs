using System.Xml.Linq;
using FlexRt.Export.ScreenEvents;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>Все 26 функций полей шага 88 против обратного XML-экспорта TIA после ручного добавления экранных объектов.</summary>
public sealed class IoFieldScreenFunctionsRunTests
{
    /// <summary>Имя шаговой копии и обратного XML-экспорта Screen_2.</summary>
    private const string Step = "88_ev_screen_object_functions";

    /// <summary>Направления и режимы ShowSlideInScreen сверены с настройками TIA и байтами реальных шагов 88–92.</summary>
    [Theory]
    [InlineData("88_ev_screen_object_functions", "top", "1")]
    [InlineData("89_slide_bottom_on", "bottom", "1")]
    [InlineData("90_slide_left_on", "left", "1")]
    [InlineData("91_slide_right_on", "right", "1")]
    [InlineData("92_slide_right_off", "right", "0")]
    public void SlideIn_DirectionAndModeAsInTia(string step, string direction, string mode)
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.IoField, $"{step}.fwc"));
        var events = new ScreenEventTexts(doc).Of(Field(doc, "G17"));
        var function = Assert.Single(events.Single(e => e.Name == "Activate").Functions);

        Assert.True(function.IsRead);
        Assert.Equal("ShowSlideInScreen", function.Name);
        Assert.Equal(["Screen name", "Mode"], function.Parameters.Select(p => p.Name));
        Assert.Equal([$"Slide-in screen {direction}", mode], function.Parameters.Select(p => p.Value));
    }

    /// <summary>
    /// Каждое поле читает Activate, функции и параметры как в TIA; неоднозначные FUNC_COMMAND сохраняют имя TIA в
    /// паре, у окна Global screen вместо отсутствующего имени — SCREEN_SYS 7, у slide-in — видимое имя экрана.
    /// </summary>
    [Fact]
    public void AllFields_FunctionsAndParametersAsInTia()
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.IoField, $"{Step}.fwc"));
        var tia = XDocument.Load(StepCopies.Path(StepCopies.IoField, $"{Step}_readback.xml"));
        var expectedFields = tia.Descendants("Hmi.Screen.IOField").ToList();
        var texts = new ScreenEventTexts(doc);
        Assert.Equal(26, expectedFields.Count);
        Assert.DoesNotContain(doc.Warnings, w => w.StartsWith("FUNC_", StringComparison.Ordinal)
            || w.StartsWith("WEB_FUNC", StringComparison.Ordinal)
            || w.StartsWith("ADAPTDEMULTIPLE", StringComparison.Ordinal)
            || w.StartsWith("SECURITY_MONITO", StringComparison.Ordinal));

        foreach (var expectedField in expectedFields)
        {
            var fieldName = expectedField.Element("AttributeList")!.Element("ObjectName")!.Value;
            var events = texts.Of(Field(doc, fieldName));
            var expectedEvent = expectedField.Element("ObjectList")!.Elements("Hmi.Event.Event").Single();
            var actualEvent = events.Single(e => e.Name == expectedEvent.Element("AttributeList")!.Element("Name")!.Value);
            var expectedFunction = expectedEvent.Descendants("Hmi.Event.FunctionListEntry").Single();
            var actualFunction = Assert.Single(actualEvent.Functions);
            var expectedName = expectedFunction.Element("AttributeList")!.Element("Name")!.Value;
            Assert.True(actualEvent.IsRead, fieldName);
            Assert.True(actualFunction.IsRead, fieldName);
            Assert.Contains(expectedName, actualFunction.Name.Split(" / "));
            Assert.Equal(ExpectedParameters(expectedFunction), actualFunction.Parameters.Select(p => $"{p.Name}={p.Value}"));
            Assert.All(events.Where(e => e.Name != actualEvent.Name), e => Assert.Empty(e.Functions));
        }
    }

    /// <summary>Параметры XML в порядке TIA с двумя установленными отличиями от экспортных имён объектов.</summary>
    /// <returns>Пары имя=значение, которые должно получить окно.</returns>
    private static string[] ExpectedParameters(XElement function) =>
    [
        .. function.Descendants("Hmi.Event.FunctionListEntryParameter").Select(p =>
        {
            var attributes = p.Element("AttributeList")!;
            var value = attributes.Element("Value")?.Value ?? p.Element("LinkList")!.Element("Value")!.Element("Name")!.Value;
            value = value switch
            {
                "SDW1" => "SCREEN_SYS 7",
                "GraphX_Slidein_Top" => "Slide-in screen top",
                _ => value
            };
            return $"{attributes.Element("Name")!.Value}={value}";
        })
    ];

    /// <summary>Поле на Screen_2 по имени объекта холста.</summary>
    /// <returns>Поле с заданным именем.</returns>
    private static IoField Field(FwxDocument doc, string name)
    {
        var screen = doc.Screens.Single(s => s.Name == "Screen_2").Index;
        var element = doc.ScreenObjects.Single(o => o.ScreenIndex == screen && o.Name == name).Element;
        return doc.IoFields.Single(f => f.Element == element);
    }
}
