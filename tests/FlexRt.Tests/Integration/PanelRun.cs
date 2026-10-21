using FlexRt.Model.Panel;

namespace FlexRt.Tests.Integration;

/// <summary>Результат одного запуска <see cref="PanelRunner.Run"/>: текст запуска (<see cref="FlexRt.Extraction.RunTranscript"/>) и панель.</summary>
/// <param name="Output">Строки уровня Info (вкладка Info окна), по строке на строку текста.</param>
/// <param name="Errors">Строки уровней Warning и Error (вкладка «Ошибки» окна), по строке на строку текста.</param>
/// <param name="Document">Прочитанная панель; <c>null</c>, если файл не прочитан.</param>
public sealed record PanelRun(string Output, string Errors, FwxDocument? Document)
{
    /// <summary>В тексте есть предупреждения или ошибки (вкладка «Ошибки» окна не пуста).</summary>
    public bool HasProblems => Errors.Length > 0;
}
