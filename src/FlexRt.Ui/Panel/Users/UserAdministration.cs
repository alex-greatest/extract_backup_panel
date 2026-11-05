using FlexRt.Model.Panel;

namespace FlexRt.Ui.Panel.Users;

/// <summary>Данные и выбор в User administration: пользователи из соседнего файла, группы и тексты из панели.</summary>
public sealed class UserAdministration(PanelUsers users, IReadOnlyList<UserGroup> groups, IReadOnlyList<LangString> strings)
{
    /// <summary>Результат чтения пользователей; при сбое список пуст.</summary>
    public PanelUsers Users { get; } = users;

    /// <summary>Группы SECURITY_UNIT в порядке панели.</summary>
    public IReadOnlyList<UserGroup> Groups { get; } = groups;

    /// <summary>Выбранный пользователь; по умолчанию — первый прочитанный.</summary>
    public PanelUser? SelectedUser { get; set; } = users.Items.Count == 0 ? null : users.Items[0];

    /// <summary>Выбранная группа вкладки User groups; по умолчанию — первая.</summary>
    public UserGroup? SelectedGroup { get; set; } = groups.Count == 0 ? null : groups[0];

    /// <summary>Открыта вкладка групп; при ошибке чтения пользователей показываются группы.</summary>
    public bool ShowGroups { get; set; } = users.Error is not null;

    /// <summary>Группа текущего пользователя по Number; служебное значение 0 не подменяется обычной группой.</summary>
    public UserGroup? MemberGroup => Groups.FirstOrDefault(group => group.Number == SelectedUser?.GroupNumber);

    /// <summary>Выбранный объект для Properties.</summary>
    public string SelectionTitle => ShowGroups
        ? SelectedGroup is { } group ? $"{DisplayName(group, null)} [Group]" : "User groups"
        : SelectedUser is { } user ? $"{user.Name} [User]" : "Users";

    /// <summary>Display name группы на выбранном языке; без выбора — все языки с различными текстами.</summary>
    /// <returns>Строка из STRINGSTORE или сообщение об отсутствии текста.</returns>
    public string DisplayName(UserGroup group, int? language)
    {
        var texts = strings.Where(text => text.Idx == group.TextIndex && (language is null || text.Lcid == language));
        var names = texts.Select(text => text.Str).Distinct().ToArray();
        return names.Length == 0 ? "неизвестно" : string.Join(" / ", names);
    }

    /// <summary>Номера прав, встречающихся у групп; стандартные три права показываются всегда.</summary>
    /// <returns>Номера прав с единицы.</returns>
    public IReadOnlyList<int> Rights() => [.. Enumerable.Range(1, 32).Where(number => number <= 3 || Groups.Any(group => HasRight(group, number)))];

    /// <summary>Название стандартного права по принятому в программе соответствию битов; нестандартное — номером.</summary>
    /// <returns>Название права.</returns>
    public static string RightName(int number) => number switch
    {
        1 => "User administration",
        2 => "Monitor",
        3 => "Operate",
        _ => $"Право {number}"
    };

    /// <summary>Включено ли право с номером от единицы в маске группы.</summary>
    /// <returns>Состояние бита маски.</returns>
    public static bool HasRight(UserGroup group, int number) => (group.Rights & (1L << (number - 1))) != 0;
}
