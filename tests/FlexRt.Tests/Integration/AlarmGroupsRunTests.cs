using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Группы алармов (байт флагов MSG_NUMEVENT и MSG_ANALOGCREAT): колонка Alarm group листов
/// «Дискретные алармы» и «Аналоговые алармы», лист «Группы алармов», строка консоли и «Сводки».
/// Образец — шаговая копия Project1 (TIA V21, TP700 Comfort), сверенная с TIA.
/// </summary>
public sealed class AlarmGroupsRunTests
{
    /// <summary>
    /// Project1: дискретные алармы 1–4 в группах 17, 1 (с Report), 2, 2; аналоговые 1 без группы,
    /// 2–7 в группе 31. Теги срабатывания дискретных алармов 2 и 3 (<c>Tag_Int</c>, <c>UInt</c>),
    /// тег квитирования HMI аларма 4 (<c>xxxx_3</c>, бит 4) и теги срабатывания аналоговых 3, 6, 7
    /// (<c>xxxx_4</c>, <c>LReal</c>, <c>HMI_Tag_1</c>) — внутренние: их записи VAR ссылаются только
    /// на AREAPTR или MSG_ANALOG_OBSE. Дискретный 4 — на абсолютном <c>%MW10</c>, бит 9; аналоговые
    /// 4–6 — пределы <c>-7</c> (Int), <c>1000</c> (DInt), <c>1.5</c> (LReal, float64); у
    /// <c>HMI_Tag_1</c> задан Display name. Строки листов сверены с TIA.
    /// </summary>
    private const string GroupsPanel = "samples/project1-groups/pdata.fwc";

    /// <summary>
    /// Project1 без данных ПЛК: «Групп алармов: 4» в консоли и на «Сводке», лист групп — ID по
    /// возрастанию и число алармов каждого вида, в колонке Alarm group — ID группы или
    /// <c>&lt;No alarm group&gt;</c>, как в TIA. Внутренний тег срабатывания — внутренний тег на
    /// листе «Теги», без предупреждения, код 0; адрес бита у внутреннего тега — номер бита, у
    /// абсолютного <c>%MW10</c> — <c>%M10.1</c>, как в TIA; запись тега с Display name читается.
    /// </summary>
    [Fact]
    public void Project1Groups_ColumnAndSheet()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo(GroupsPanel), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Групп алармов: 4");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Групп алармов=4");
        Assert.Equal(["ID | Discrete alarms | Analog alarms", "1 | 1 | 0", "2 | 2 | 0", "17 | 1 | 0", "31 | 0 | 6"],
            AlarmGroupsSheetDump.Read(dir.PanelDataWorkbook));
        var discrete = DiscreteAlarmsSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Contains(" | Alarm class | Alarm group | Trigger tag | ", discrete[0]);
        Assert.Equal("1 | vvvvrrre | Acknowledgement | 17 | UInt_IW14 | 5 | неизвестно | <No tag> | 0 |  | <No tag> | 0 | Off", discrete[1]);
        Assert.Equal("2 |  | Errors | 1 | Tag_Int | 0 | 0 | <No tag> | 0 |  | <No tag> | 0 | On", discrete[2]);
        Assert.Equal("3 |  | Errors | 2 | UInt | 0 | 0 | <No tag> | 0 |  | <No tag> | 0 | Off", discrete[3]);
        Assert.Equal("4 |  | Acknowledgement | 2 | xxxx_5 | 9 | %M10.1 | xxxx_3 | 4 | 4 | <No tag> | 0 | Off", discrete[4]);
        var tags = TagSheetDump.Read(dir.PanelDataWorkbook);
        Assert.Contains("A1=Всего тегов | B1=72", tags);
        Assert.Contains("A2=Внутренних тегов | B2=22", tags);
        Assert.Contains("A3=Тегов с ПЛК | B3=50", tags);
        Assert.Contains("A12=Tag_Int | B12=Int | C12=<Internal tag>", tags);
        Assert.Contains("A20=UInt | B20=UInt | C20=<Internal tag>", tags);
        Assert.Contains("A79=xxxx_3 | B79=Int | C79=<Internal tag>", tags);
        Assert.Contains("A80=xxxx_4 | B80=Real | C80=<Internal tag>", tags);
        Assert.Contains("A82=HMI_Tag_1 | B82=Real | C82=<Internal tag>", tags);
        Assert.Equal(
        [
            "ID | 0x409 — English (United States) | Alarm class | Alarm group | Trigger tag | Limit | Limit mode | Report",
            "1 |  | Errors | <No alarm group> | DInt_ID6 | 111 | Higher | Off",
            "2 |  | Errors | 31 | DInt_ID6 | 22 | Higher | Off",
            "3 |  | Errors | 31 | xxxx_4 | 5000 | Higher | Off",
            "4 |  | Errors | 31 | Int_IW30 | -7 | Higher | Off",
            "5 |  | Errors | 31 | DInt_ID6 | 1000 | Higher | Off",
            "6 |  | Errors | 31 | LReal | 1.5 | Higher | Off",
            "7 |  | Errors | 31 | HMI_Tag_1 | 10 | Higher | Off"
        ], AnalogAlarmsSheetDump.Read(dir.PanelDataWorkbook));
    }

    /// <summary><c>samples/pdata.fwc</c> без алармов: «Групп алармов: 0» в консоли и на «Сводке», листа групп нет.</summary>
    [Fact]
    public void SamplePanel_NoGroups_NoSheet()
    {
        using var dir = new TestDirectory();
        var run = FlexRtProcess.Run(RepositoryPaths.InRepo("samples/pdata.fwc"), dir.MissingFile(), dir.OutDir);

        RunAssertions.ExitCode(run, 0);
        RunAssertions.HasLine(run.StdOut, "Групп алармов: 0");
        SummarySheetDump.Contains(dir.PanelDataWorkbook, "Групп алармов=0");
        AlarmGroupsSheetDump.Absent(dir.PanelDataWorkbook);
    }
}
