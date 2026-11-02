using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Положение объектов экранов (<see cref="FwxDocument.ScreenObjects"/>, холст окна) против Layout TIA: A603A0097
/// (выгрузка экранов через Openness), <c>samples/screens/05_popup.fwc</c>, шаговые копии Project1
/// <c>data/real/steps-objects/</c> (вне git) с Trend view и f(x) trend view; испорченные элементы.
/// </summary>
public sealed class ScreenObjectsRunTests
{
    /// <summary>
    /// A603A0097 (TP1900 Comfort, V21): объекты разных видов сошлись с X, Y, Width, Height выгрузки TIA (2026-11-02, все
    /// 396 объектов 13 экранов и 17 объектов экранов 200–202): кнопка, текст, поле ввода-вывода, объект в группе
    /// (VIEW_MOVE), объект с анимацией видимости (VIEW_DARK), линия, круг (2 × Radius), f(x) trend view (Layout в
    /// TIA); предупреждений нет.
    /// </summary>
    [Fact]
    public void A603_ObjectsAsInTia()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/a603/pdata.fwc"));

        Assert.Empty(doc.Warnings);
        Assert.Equal(new ScreenRect(640, 419, 155, 55), Bounds(doc, "001_AUTO_SCREEN", "Button_1"));
        Assert.Equal(new ScreenRect(2, 443, 70, 29), Bounds(doc, "001_AUTO_SCREEN", "Text field_7"));
        Assert.Equal(new ScreenRect(90, 442, 544, 32), Bounds(doc, "001_AUTO_SCREEN", "I/O field_4"));
        Assert.Equal(new ScreenRect(117, 62, 681, 40), Bounds(doc, "001_AUTO_SCREEN", "Text Field_8"));
        Assert.Equal(new ScreenRect(205, 185, 340, 200), Bounds(doc, "001_AUTO_SCREEN", "Graphic I/O field_1"));
        Assert.Equal(new ScreenRect(730, 64, 70, 70), Bounds(doc, "410_I_S01", "Exit"));
        Assert.Equal(new ScreenRect(9, 194, 782, 0), Bounds(doc, "410_I_S01", "Line_1"));
        Assert.Equal(new ScreenRect(757, 160, 30, 30), Bounds(doc, "410_I_S01", "Circle_1"));
        Assert.Equal(new ScreenRect(0, 134, 800, 345), Bounds(doc, "202_RACK PULL CCW_CW", "f(x) trend view_1"));
    }

    /// <summary>
    /// Шаг 05 Project1: кнопки шаблона внизу Root screen, кнопка pop-up, объекты Permanent area (запись
    /// <c>Overview</c>) от угла панели; рабочая область VIEW_DYN — не объект.
    /// </summary>
    [Fact]
    public void Popup_TemplatePopupAndPermanentArea()
    {
        var doc = FwxReader.Read(RepositoryPaths.InRepo("samples/screens/05_popup.fwc"));

        Assert.Empty(doc.Warnings);
        Assert.Equal(new ScreenRect(720, 360, 78, 58), Bounds(doc, "Root screen", "Exit"));
        Assert.Equal(new ScreenRect(1, 360, 78, 58), Bounds(doc, "Root screen", "Start screen"));
        Assert.Equal(new ScreenRect(86, 56, 96, 32), Bounds(doc, "Pop-up screen_1", "Button_1"));
        Assert.Equal(new ScreenRect(0, 0, 107, 60), Bounds(doc, "Overview", "Logo"));
        Assert.Equal(13, Objects(doc, "Root screen").Count);
        Assert.DoesNotContain(doc.ScreenObjects, o => o.Table == "VIEW_DYN");
    }

    /// <summary>
    /// Trend view — рамкой соседнего объекта без имени в VIEW_MOVE (сам он не выводится), f(x) trend view — рамкой из
    /// сжатых данных; значения — Layout TIA на снимках пользователя (2026-11-02).
    /// </summary>
    [Theory]
    [InlineData("01_trend_view.fwc", "Screen_2", "Trend view_1", 130, 0, 600, 300)]
    [InlineData("02_trend_view_screen1.fwc", "Screen_1", "Trend view_1", 97, 104, 449, 149)]
    [InlineData("03_trend_view_root.fwc", "Root screen", "Trend view_1", 599, 173, 201, 136)]
    [InlineData("04_fx_trend_view.fwc", "Root screen", "f(x) trend view_1", 0, 81, 600, 300)]
    public void Steps_TrendsAsInTia(string step, string screen, string name, int left, int top, int width, int height)
    {
        var doc = FwxReader.Read(StepCopies.Path(StepCopies.Objects, step));

        Assert.Empty(doc.Warnings);
        Assert.Equal(new ScreenRect(left, top, width, height), Bounds(doc, screen, name));
        Assert.DoesNotContain(Objects(doc, screen), o => o.Name.Length == 0);
    }

    /// <summary>
    /// A603A0097, число объектов первого VIEW_MOVE (группа экрана <c>002_NEXT_STEP</c>) — 0x40: одно предупреждение с
    /// таблицей и смещением, объекты вне группы читаются.
    /// </summary>
    [Fact]
    public void BrokenGroup_WarningAndOtherObjects()
    {
        using var dir = new TestDirectory();
        // +0x0a — u16 число пар: u32, u32 R = 0, u16
        var (input, item) = CorruptedCopy.U16(dir, "samples/a603/pdata.fwc", "VIEW_MOVE", 0, 0x0a, 0x40);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"VIEW_MOVE @ 0x{item.Offset:X}: запись 1: 64 объектов не помещаются в {item.Length} байт"], doc.Warnings);
        Assert.Equal(new ScreenRect(640, 419, 155, 55), Bounds(doc, "001_AUTO_SCREEN", "Button_1"));
    }

    /// <summary>
    /// A603A0097, номер элемента в паре первого VIEW_DARK — 0xFFFF: эта обёртка (объект шаблона <c>Text field_1</c>)
    /// стоит на многих экранах, сообщение о ссылке за последний элемент содержит имя экрана — но предупреждение одно
    /// (одно на элемент), объекта нет ни на одном экране, остальные объекты читаются.
    /// </summary>
    [Fact]
    public void BrokenTemplateLink_OneWarningPerElement()
    {
        using var dir = new TestDirectory();
        // +0x10 — пара за ссылками: u32, u32 R = 1, ссылка 8 байт; u16 номер элемента, u16 Id таблицы
        var (input, _) = CorruptedCopy.U16(dir, "samples/a603/pdata.fwc", "VIEW_DARK", 0, 0x10, 0xFFFF);
        var doc = FwxReader.Read(input);

        var warning = Assert.Single(doc.Warnings);
        Assert.Contains(": экран ", warning);
        Assert.Contains("ссылается на элемент 65536, а элементов", warning);
        Assert.DoesNotContain(doc.ScreenObjects, o => o.Name == "Text field_1" && o.Bounds == new ScreenRect(60, 7, 107, 52));
        Assert.Equal(new ScreenRect(640, 419, 155, 55), Bounds(doc, "001_AUTO_SCREEN", "Button_1"));
    }

    /// <summary>
    /// A603A0097, первое слово объекта шаблона <c>Template_I/O field_2</c> (VIEW_OUT 6, на многих экранах) — 5: одно
    /// предупреждение «положение не прочитано» (одно на элемент), объекта нет ни на одном экране.
    /// </summary>
    [Fact]
    public void TemplateObjectNotRead_OneWarningPerElement()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U16(dir, "samples/a603/pdata.fwc", "VIEW_OUT", 5, 0x00, 5);
        var doc = FwxReader.Read(input);

        var warning = Assert.Single(doc.Warnings);
        Assert.StartsWith($"VIEW_OUT @ 0x{item.Offset:X}: запись 6: объект Template_I/O field_2 экрана ", warning);
        Assert.EndsWith(": положение не прочитано", warning);
        Assert.DoesNotContain(doc.ScreenObjects, o => o.Name == "Template_I/O field_2");
    }

    /// <summary>
    /// Шаг 05, первое слово элемента кнопки Exit — 5: положение не прочитано — предупреждение с таблицей и смещением,
    /// объекта нет, остальные объекты экрана есть.
    /// </summary>
    [Fact]
    public void UnknownKind_PositionNotRead()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U16(dir, "samples/screens/05_popup.fwc", "VIEW_BUTTONS", 0, 0x00, 5);
        var doc = FwxReader.Read(input);

        Assert.Equal([$"VIEW_BUTTONS @ 0x{item.Offset:X}: запись 1: объект Exit экрана Root screen: положение не прочитано"], doc.Warnings);
        Assert.DoesNotContain(Objects(doc, "Root screen"), o => o.Name == "Exit");
        Assert.Equal(12, Objects(doc, "Root screen").Count);
    }

    /// <summary>
    /// Шаг 05, Right кнопки Exit — 900 (+0x20: имя 4 символа, R = 1, ссылка, стиль, Left, Top): объект с этой рамкой
    /// есть (рисуется) и предупреждение, что рамка выходит за экран.
    /// </summary>
    [Fact]
    public void FrameOutsideScreen_ObjectAndWarning()
    {
        using var dir = new TestDirectory();
        var (input, item) = CorruptedCopy.U16(dir, "samples/screens/05_popup.fwc", "VIEW_BUTTONS", 0, 0x20, 900);
        var doc = FwxReader.Read(input);

        Assert.Equal(
            [$"VIEW_BUTTONS @ 0x{item.Offset:X}: запись 1: объект Exit экрана Root screen: рамка 720, 360, 180×58 выходит за экран 800×420"],
            doc.Warnings);
        Assert.Equal(new ScreenRect(720, 360, 180, 58), Bounds(doc, "Root screen", "Exit"));
    }

    /// <summary>Объекты экрана по имени экрана.</summary>
    /// <returns>Объекты в порядке документа.</returns>
    private static List<ScreenObject> Objects(FwxDocument doc, string screen)
    {
        var index = doc.Screens.Single(s => s.Name == screen).Index;
        return [.. doc.ScreenObjects.Where(o => o.ScreenIndex == index)];
    }

    /// <summary>Положение единственного объекта с этим именем на экране.</summary>
    /// <returns>Положение и размер.</returns>
    private static ScreenRect Bounds(FwxDocument doc, string screen, string name) =>
        Objects(doc, screen).Single(o => o.Name == name).Bounds;
}
