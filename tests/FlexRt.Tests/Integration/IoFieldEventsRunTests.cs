using System.Xml.Linq;
using FlexRt.Export.ScreenEvents;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>События I/O field против XML TIA: реальные шаги 82–87, Output и повреждение одной функции.</summary>
public sealed class IoFieldEventsRunTests
{
    /// <summary>События, порядок функций, имена и значения параметров совпадают с заданными в XML TIA.</summary>
    [Theory]
    [InlineData("82_ev_activate_setbit")]
    [InlineData("83_ev_deactivate_setbit")]
    [InlineData("84_ev_finishinput_setbit")]
    [InlineData("85_ev_three_lists")]
    [InlineData("87_ev_object_functions")]
    public void Steps_EventsAsInTia(string step)
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.IoField, $"{step}.fwc"));
        var tia = XDocument.Load(StepCopies.Path(StepCopies.IoField, $"{step}.xml"));
        var texts = new ScreenEventTexts(doc);
        Assert.Empty(doc.Warnings);

        foreach (var expectedField in tia.Descendants("Hmi.Screen.IOField"))
        {
            var name = expectedField.Element("AttributeList")!.Element("ObjectName")!.Value;
            var events = texts.Of(Field(doc, name));
            Assert.Equal(["Activate", "Deactivate", "OnFinishInput"], events.Select(e => e.Name));
            foreach (var actualEvent in events)
            {
                var expectedEvent = expectedField.Element("ObjectList")!.Elements("Hmi.Event.Event")
                    .SingleOrDefault(e => e.Element("AttributeList")!.Element("Name")!.Value == actualEvent.Name);
                Assert.True(actualEvent.IsRead, $"{step}: {name} {actualEvent.Name}");
                Assert.Equal(ExpectedFunctions(expectedEvent), actualEvent.Functions.Select(Line));
            }
        }
    }

    /// <summary>Шаг 86: в XML Output заданы три события с пятью функциями, в FWC этих событий поля нет.</summary>
    [Fact]
    public void Output_XmlEventsAreNotCompiledIntoField()
    {
        const string step = "86_ev_output_three_lists";
        var tia = XDocument.Load(StepCopies.Path(StepCopies.IoField, $"{step}.xml"))
            .Descendants("Hmi.Screen.IOField").Single();
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.IoField, $"{step}.fwc"));
        var field = Field(doc, "I/O field_1");

        Assert.Empty(doc.Warnings);
        Assert.Equal("Output", tia.Element("AttributeList")!.Element("Mode")!.Value);
        Assert.Equal(3, tia.Element("ObjectList")!.Elements("Hmi.Event.Event").Count());
        Assert.Equal(5, tia.Descendants("Hmi.Event.FunctionListEntry").Count());
        Assert.Equal(IoFieldMode.Output, field.Mode);
        Assert.Empty(field.Events ?? []);
        Assert.All(new ScreenEventTexts(doc).Of(field), e => Assert.Empty(e.Functions));
    }

    /// <summary>Повреждение SetBit в Activate шага 85 не скрывает поле, вторую функцию и остальные события.</summary>
    [Fact]
    public void BrokenFunction_FieldAndOtherCallsRemain()
    {
        const string step = "85_ev_three_lists.fwc";
        var source = FwxReader.Read(StepCopies.Path(StepCopies.IoField, step));
        var activate = Field(source, "I/O field_1").Events!.Single(e => e.Kind == ObjectEventKind.Activate);
        var first = activate.Functions![0].Element;
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U32(dir, $"{StepCopies.IoField}/{step}", "FUNC_BIT", first.Index, 0x00, 7);

        var doc = FwxReader.Read(input);
        var events = new ScreenEventTexts(doc).Of(Field(doc, "I/O field_1"));

        Assert.Equal([$"FUNC_BIT @ 0x{item.Offset:X}: запись {first.Index + 1}: первое слово 7, ожидалось 1"], doc.Warnings);
        Assert.Equal([ScreenEventTexts.NotRead, "ResetBit Tag=Bool_I1_5"], events[0].Functions.Select(Line));
        Assert.Equal(["InvertBit Tag=Bool_I1_5"], events[1].Functions.Select(Line));
        Assert.Equal(["SetBit Tag=Bool_I1_5", "ResetBit Tag=Bool_I0_0"], events[2].Functions.Select(Line));
    }

    /// <summary>Поле Screen_2 по ссылке объекта холста; одинаковые имена в шаблонах не смешиваются.</summary>
    /// <returns>Поле с заданным именем.</returns>
    private static IoField Field(FwxDocument doc, string name)
    {
        var screen = doc.Screens.Single(s => s.Name == "Screen_2").Index;
        var element = doc.ScreenObjects.Single(o => o.ScreenIndex == screen && o.Name == name).Element;
        return doc.IoFields.Single(f => f.Element == element);
    }

    /// <summary>Функции XML в порядке TIA; значения параметров — ссылки по имени или константы.</summary>
    /// <returns>Строки функций с параметрами.</returns>
    private static string[] ExpectedFunctions(XElement? expectedEvent) =>
        expectedEvent is null ? [] : [.. expectedEvent.Descendants("Hmi.Event.FunctionListEntry").Select(f =>
            string.Join(" ", [f.Element("AttributeList")!.Element("Name")!.Value,
                .. f.Descendants("Hmi.Event.FunctionListEntryParameter").Select(p =>
                    $"{p.Element("AttributeList")!.Element("Name")!.Value}={p.Element("AttributeList")!.Element("Value")?.Value ?? p.Element("LinkList")!.Element("Value")!.Element("Name")!.Value}")]))];

    /// <summary>Функция окна одной строкой, без порядкового номера.</summary>
    /// <returns>Имя функции и пары имя=значение.</returns>
    private static string Line(FunctionView function) =>
        string.Join(" ", [function.Name, .. function.Parameters.Select(p => $"{p.Name}={p.Value}")]);
}
