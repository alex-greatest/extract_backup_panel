using System.Text.Json;
using FlexRt.Parsing.Panel.Functions;
using Xunit;

namespace FlexRt.Tests.Unit.Panel;

/// <summary>Словарь системных функций <c>SystemFunctions.json</c> (ресурс ядра): читается, ключи не повторяются, ошибки словаря ловятся.</summary>
public sealed class SystemFunctionCatalogTests
{
    /// <summary>Ресурс читается целиком: 134 пары «таблица + подкод» (142 функции TIA, пары с одинаковыми байтами — одной записью).</summary>
    [Fact]
    public void Resource_LoadsAllEntries()
    {
        Assert.Equal(134, SystemFunctionCatalog.Count);
        Assert.Equal(["SetBit"], SystemFunctionCatalog.Find("FUNC_BIT", 1)!.Names);
        Assert.Equal(["HTMLBrowserPageUp", "ScreenObjectPageUp"], SystemFunctionCatalog.Find("FUNC_COMMAND", 0x1002)!.Names);
        Assert.Null(SystemFunctionCatalog.Find("FUNC_BIT", 7));
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
