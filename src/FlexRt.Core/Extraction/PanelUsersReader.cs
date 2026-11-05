using FlexRt.Model.Panel;
using FlexRt.Parsing.Panel;

namespace FlexRt.Extraction;

/// <summary>Поиск PWX и PWL с любым суффиксом рядом с панелью; используется первый полностью прочитанный файл.</summary>
public static class PanelUsersReader
{
    /// <summary>Прочитать соседние файлы по очереди; сбой кандидата не мешает попробовать следующий.</summary>
    /// <returns>Первый успешный список либо ошибка без пользователей; файлы не записываются.</returns>
    public static PanelUsers Read(string panelPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(panelPath))!;
            List<string> failures = [];
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var extension = Path.GetExtension(path);
                if (!extension.Equals(".pwx", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains(".pwl", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                try
                {
                    return new PanelUsers(path, UserFileParser.Read(path), null);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or Binary.FwxFormatException)
                {
                    failures.Add($"{Path.GetFileName(path)}: {error.Message}");
                }
            }
            var reason = failures.Count == 0
                ? "рядом с файлом панели нет файлов .pwx или .pwl*"
                : string.Join(Environment.NewLine, failures);
            return new PanelUsers(null, [], $"Пользователи не прочитаны: {reason}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return new PanelUsers(null, [], $"Пользователи не прочитаны: {error.Message}");
        }
    }
}
