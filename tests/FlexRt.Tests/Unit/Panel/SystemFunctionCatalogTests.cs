using System.Text.Json;
using FlexRt.Parsing.Panel.Functions;
using Xunit;

namespace FlexRt.Tests.Unit.Panel;

/// <summary>Словарь системных функций <c>SystemFunctions.json</c> (ресурс ядра): читается, ключи не повторяются, ошибки словаря ловятся.</summary>
public sealed class SystemFunctionCatalogTests
{
    /// <summary>Ресурс читается целиком: 146 пар «таблица + подкод», включая функции I/O field шагов 87 и 88.</summary>
    [Fact]
    public void Resource_LoadsAllEntries()
    {
        Assert.Equal(146, SystemFunctionCatalog.Count);
        Assert.Equal(["SetBit"], SystemFunctionCatalog.Find("FUNC_BIT", 1)!.Names);
        Assert.Equal(["HTMLBrowserPageUp", "ScreenObjectPageUp"], SystemFunctionCatalog.Find("FUNC_COMMAND", 0x1002)!.Names);
        Assert.Null(SystemFunctionCatalog.Find("FUNC_BIT", 7));
    }

    /// <summary>Шесть новых пар шага 88: имя функции и полный список параметров в порядке TIA.</summary>
    [Theory]
    [InlineData("FUNC_PRODIAG", 0, "ActivatePLCCodeView", "Screen name|Screen object")]
    [InlineData("FUNC_PLCCODEVIE", 1, "PLCCodeViewMoveScrollbar", "Screen object|Area|Direction")]
    [InlineData("FUNC_SYSDIAG", 0, "ShowSystemDiagnosticsWindow", "Object name")]
    [InlineData("FUNC_SYSDIAG", 1, "ActivateSystemDiagnosticsView", "Screen name|Object name")]
    [InlineData("FUNC_SHOWPOPUPS", 0, "ShowPopupScreen", "Name of the screen|X coordinate|Y coordinate|Display mode|Animation|Animation speed")]
    [InlineData("FUNC_SHOWSLIDEI", 0, "ShowSlideInScreen", "Screen name|Mode")]
    public void Step88_EntriesHaveNamesAndParameters(string table, long subcode, string name, string parameters)
    {
        var entry = SystemFunctionCatalog.Find(table, subcode);

        Assert.NotNull(entry);
        Assert.Contains(name, entry.Names);
        Assert.Equal(parameters.Split('|'), entry.Parameters);
    }

    /// <summary>Команды PLC code view совпадают по подкоду с RecipeView: альтернативное имя сохраняется, не подменяя старое.</summary>
    [Theory]
    [InlineData(1, "RecipeViewSaveDataRecord", "PLCCodeViewSymbols")]
    [InlineData(2, "RecipeViewNewDataRecord", "PLCCodeViewPreviousNetwork")]
    [InlineData(3, "RecipeViewClearDataRecord", "PLCCodeViewNextNetwork")]
    [InlineData(4, "RecipeViewGetDataRecordFromPLC", "PLCCodeViewZoomIn")]
    [InlineData(5, "RecipeViewSetDataRecordToPLC", "PLCCodeViewZoomOut")]
    [InlineData(6, "RecipeViewSynchronizeDataRecordWithTags", "PLCCodeViewDetails")]
    [InlineData(7, "RecipeViewSaveAsDataRecord", "PLCCodeViewStepMode")]
    [InlineData(8, "RecipeViewShowOperatorNotes", "PLCCodeViewTransitionInterlock")]
    public void Step88_PlcCodeViewNamesDoNotReplaceRecipeView(long subcode, string recipe, string plcCodeView)
    {
        var entry = SystemFunctionCatalog.Find("FUNC_COMMAND", subcode);

        Assert.NotNull(entry);
        Assert.Contains(recipe, entry.Names);
        Assert.Contains(plcCodeView, entry.Names);
        Assert.Equal(recipe, entry.ObjectReferences!["RECIPE_FACTORY"]);
    }

    /// <summary>Пара «таблица + подкод» повторяется — ошибка словаря.</summary>
    [Fact]
    public void DuplicateKey_Throws()
    {
        const string json = """
            [
              {"table": "FUNC_BIT", "subcode": "0x1", "names": ["SetBit"], "parameters": ["Tag"]},
              {"table": "FUNC_BIT", "subcode": "0x1", "names": ["ResetBit"], "parameters": ["Tag"]}
            ]
            """;

        var e = Assert.Throws<InvalidOperationException>(() => SystemFunctionCatalog.Parse(json));
        Assert.Equal("словарь функций: FUNC_BIT 0x1 повторяется", e.Message);
    }

    /// <summary>Поле записи повторено или не известно — ошибка разбора JSON.</summary>
    [Theory]
    [InlineData("""[{"table": "FUNC_BIT", "subcode": "0x1", "names": ["SetBit"], "names": ["X"], "parameters": []}]""")]
    [InlineData("""[{"table": "FUNC_BIT", "subcode": "0x1", "name": ["SetBit"], "parameters": []}]""")]
    public void BadField_Throws(string json) =>
        Assert.ThrowsAny<JsonException>(() => SystemFunctionCatalog.Parse(json));

    /// <summary>Подкод не <c>0x…</c> или выбор по объекту ведёт не на имя записи — ошибка словаря.</summary>
    [Theory]
    [InlineData("""[{"table": "FUNC_BIT", "subcode": "1", "names": ["SetBit"], "parameters": []}]""")]
    [InlineData("""[{"table": "FUNC_COMMAND", "subcode": "0x1", "names": ["A", "B"], "parameters": [], "objectReferences": {"RECIPE_FACTORY": "C"}}]""")]
    public void BadEntry_Throws(string json) =>
        Assert.Throws<InvalidOperationException>(() => SystemFunctionCatalog.Parse(json));
}
