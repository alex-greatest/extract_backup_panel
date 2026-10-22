using System.Runtime.CompilerServices;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Export.PanelData.Screens;

/// <summary>Таблица листа «Экраны»: экраны панели с номером, признаком стартового, размером, цветом фона, подсказкой и видом.</summary>
public static class ScreensTable
{
    /// <summary>Вид обычного экрана: у него есть номер (Screen number в TIA).</summary>
    private const string ScreenKind = "Screen";

    /// <summary>Вид pop-up экрана: на запись ссылается служебная запись SCREEN.</summary>
    private const string PopupKind = "Pop-up screen";

    /// <summary>Вид slide-in экрана: одно из четырёх имён, которые задаёт TIA и не даёт изменить.</summary>
    private const string SlideInKind = "Slide-in screen";

    /// <summary>Вид и имя записи 0 без номера (в файле — <c>Overview</c>): в ней объекты Permanent area (сверено на Project1).</summary>
    private const string PermanentAreaKind = "Permanent area";

    /// <summary>Значение, которое не удалось определить: вид экрана или номер, если ADAPTVALUECHANG прочитана не целиком.</summary>
    private const string Unknown = "?";

    /// <summary>Значение колонки Start screen у стартового экрана.</summary>
    private const string StartScreenMark = "Yes";

    /// <summary>Заголовки колонок перед колонками подсказки.</summary>
    private static readonly string[] HeadColumns = ["Name", "Number", "Start screen", "Width", "Height", "Background color"];

    /// <summary>Заголовок колонки вида — последней, после колонок подсказки.</summary>
    private const string KindColumn = "Вид";

    /// <summary>Уже построенные таблицы: окно (экраны) и лист «Экраны» берут одну таблицу документа.</summary>
    private static readonly ConditionalWeakTable<FwxDocument, ReportTable> Built = [];

    /// <summary>Имена slide-in экранов: TIA задаёт их сам, переименовать нельзя; во всех известных файлах английские.</summary>
    private static readonly HashSet<string> SlideInNames = ["Slide-in screen top", "Slide-in screen left", "Slide-in screen bottom", "Slide-in screen right"];

    /// <summary>
    /// Экраны панели без служебных записей, с видом и номером. Вид: есть номер — <c>Screen</c>; на запись ссылается
    /// служебная запись — <c>Pop-up screen</c>; имя slide-in — <c>Slide-in screen</c>; запись 0 —
    /// <c>Permanent area</c>; иначе <c>?</c>. Порядок: Permanent area, обычные экраны по возрастанию номера,
    /// затем остальные в порядке файла (порядок записей в файле компилятор меняет).
    /// </summary>
    /// <returns>Запись SCREEN, вид и номер экрана (<c>null</c> — номера нет).</returns>
    public static IReadOnlyList<(Screen Screen, string Kind, int? Number)> Screens(FwxDocument doc)
    {
        var popups = PopupIndexes(doc);
        return
        [
            .. doc.Screens
                .Where(s => !s.IsService)
                .Select(s => (Screen: s, Number: NumberOf(doc, s.Index)))
                .Select(s => (s.Screen, Kind: KindOf(s.Screen, s.Number, popups), s.Number))
                .OrderBy(s => KindRank(s.Kind))
                .ThenBy(s => s.Number)
                .ThenBy(s => s.Screen.Index)
        ];
    }

    /// <summary>Число обычных экранов — экранов с номером.</summary>
    /// <returns>Число строк вида <c>Screen</c>.</returns>
    public static int ScreenCount(FwxDocument doc) => Screens(doc).Count(s => s.Kind == ScreenKind);

    /// <summary>
    /// Построить таблицу: <c>Name</c>, <c>Number</c> (числом; нет номера — пусто, а если ADAPTVALUECHANG прочитана
    /// не целиком — <c>?</c>), <c>Start screen</c> (<c>Yes</c> у экрана с номером из GUI), <c>Width</c>,
    /// <c>Height</c>, <c>Background color</c> (<c>182, 182, 182</c> — R, G, B, как в TIA, с заливкой ячейки этим цветом;
    /// цвет не прочитан — <c>?</c> без заливки), <c>Tooltip &lt;язык&gt;</c>
    /// на каждый язык панели (<see cref="ScreenTooltips"/>), <c>Вид</c>; строка на экран из <see cref="Screens"/>. У
    /// Permanent area имя — <c>Permanent area</c>, а не имя из файла.
    /// </summary>
    /// <returns>Заголовки и строки экранов; колонки подсказки помечены языком и группой <c>Tooltip</c>.</returns>
    /// <remarks>
    /// Таблица строится при первом вызове для документа и запоминается: документ после чтения не меняется, а таблицу
    /// берут и экраны окна, и лист «Экраны».
    /// </remarks>
    public static ReportTable Build(FwxDocument doc) => Built.GetValue(doc, BuildTable);

    /// <summary>Построить таблицу листа «Экраны» (<see cref="Build"/>).</summary>
    /// <returns>Заголовки и строки экранов.</returns>
    private static ReportTable BuildTable(FwxDocument doc)
    {
        var tooltips = new ScreenTooltips(doc);
        List<ReportColumn> columns = [.. HeadColumns.Select(c => new ReportColumn(c)), .. tooltips.Columns(), new(KindColumn)];
        var rows = Screens(doc).Select(s => (ReportCell[])
        [
            s.Kind == PermanentAreaKind ? PermanentAreaKind : s.Screen.Name,
            NumberCell(doc, s.Number),
            s.Number is not null && s.Number == doc.StartScreenNumber ? StartScreenMark : "",
            s.Screen.Width,
            s.Screen.Height,
            ColorCell(s.Screen.BackgroundRgb),
            .. tooltips.Cells(s.Screen.Index),
            s.Kind
        ]).ToList();
        return ReportTable.WithLanguages(columns, rows);
    }

    /// <summary>Номер экрана записи SCREEN: первая пара из ADAPTVALUECHANG с этой записью.</summary>
    /// <returns>Номер или <c>null</c>, если пары нет.</returns>
    private static int? NumberOf(FwxDocument doc, int screenIndex) =>
        doc.ScreenNumbers.Where(n => n.ScreenIndex == screenIndex).Select(n => (int?)n.Number).FirstOrDefault();

    /// <summary>Номера записей SCREEN, на которые ссылаются объекты служебных записей (Id таблицы SCREEN из TOC): pop-up экраны.</summary>
    /// <returns>Номера записей pop-up экранов.</returns>
    private static HashSet<int> PopupIndexes(FwxDocument doc)
    {
        var screenTable = doc.FindTable("SCREEN")?.Id;
        return
        [
            .. doc.Screens
                .Where(s => s.IsService)
                .SelectMany(s => s.Objects)
                .Where(o => o.Table == screenTable)
                .Select(o => o.Index)
        ];
    }

    /// <summary>Вид экрана по номеру, ссылкам служебных записей, имени и номеру записи.</summary>
    /// <returns>Текст колонки Вид.</returns>
    private static string KindOf(Screen screen, int? number, HashSet<int> popups)
    {
        if (number is not null)
        {
            return ScreenKind;
        }
        if (popups.Contains(screen.Index))
        {
            return PopupKind;
        }
        if (SlideInNames.Contains(screen.Name))
        {
            return SlideInKind;
        }
        return screen.Index == 0 ? PermanentAreaKind : Unknown;
    }

    /// <summary>Место вида в порядке строк: Permanent area, обычные экраны, остальные.</summary>
    /// <returns>0, 1 или 2.</returns>
    private static int KindRank(string kind) => kind switch
    {
        PermanentAreaKind => 0,
        ScreenKind => 1,
        _ => 2
    };

    /// <summary>Ячейка Number: номер числом; номера нет — пусто, а если номера прочитаны не целиком — <c>?</c>.</summary>
    /// <returns>Ячейка таблицы.</returns>
    private static ReportCell NumberCell(FwxDocument doc, int? number)
    {
        if (number is { } n)
        {
            return n;
        }
        return doc.ScreenNumbersComplete ? "" : Unknown;
    }

    /// <summary>
    /// Ячейка Background color: <c>R, G, B</c> десятичными, как в свойствах экрана в TIA, с заливкой этим цветом
    /// (<see cref="RgbText.Cell"/>); цвет не прочитан — <c>?</c> без заливки.
    /// </summary>
    /// <returns>Ячейка таблицы.</returns>
    private static ReportCell ColorCell(int? rgb) => rgb is { } color ? RgbText.Cell(color) : Unknown;
}
