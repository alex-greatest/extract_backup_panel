using FlexRt.Parsing.Panel;
using Xunit;

namespace FlexRt.Tests.Integration;

/// <summary>
/// Шаговые копии <c>data/real/steps/</c> (TIA V17, внутренние теги; вне git): в каждой следующей копии пользователь
/// добавил один тег. Проверка разбора <c>VAR</c> из правил «Проверки перед завершением» в <c>CLAUDE.md</c>.
/// </summary>
public sealed class RealStepsTests
{
    /// <summary>
    /// Копии в порядке имён (<c>01_Tag_Int.fwc</c>, <c>02_Bool.fwc</c>, …): ни одного предупреждения разбора, тегов на 1
    /// больше, чем в предыдущей. Папки нет (копии не в git) — провал с понятным сообщением, а не молчаливый успех.
    /// </summary>
    [Fact]
    public void EachStep_AddsOneTagWithoutWarnings()
    {
        var steps = Directory.GetFiles(StepCopies.Folder(StepCopies.Tags), "*.fwc").Order(StringComparer.Ordinal).ToList();
        Assert.True(steps.Count > 1, $"в {StepCopies.Tags} меньше двух копий");

        int? previous = null;
        foreach (var step in steps)
        {
            var doc = FwxReader.Read(step);
            var name = Path.GetFileName(step);
            Assert.True(doc.Warnings.Count == 0, $"{name}: предупреждения разбора:\n{string.Join("\n", doc.Warnings)}");
            if (previous is { } count)
            {
                Assert.True(doc.Tags.Count == count + 1, $"{name}: тегов {doc.Tags.Count}, ожидалось {count + 1}");
            }
            previous = doc.Tags.Count;
        }
    }
}
