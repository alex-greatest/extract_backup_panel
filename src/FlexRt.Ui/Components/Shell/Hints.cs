namespace FlexRt.Ui.Components.Shell;

/// <summary>Подсказки окна на месте пустого дерева, холста и панели свойств.</summary>
public static class Hints
{
    /// <summary>Файл не открыт (или не прочитан): дерево, холст и панель свойств.</summary>
    public const string NoFile = "Откройте pdata.fwc";

    /// <summary>Файл читается.</summary>
    public const string Reading = "Чтение файла…";

    /// <summary>Панель открыта, но ничего не выбрано.</summary>
    public const string NoScreen = "Выберите экран в дереве проекта";

    /// <summary>Панель свойств: открыта таблица, а строка не выбрана.</summary>
    public const string NoRow = "Выберите строку таблицы";

    /// <summary>Панель свойств: раздел, таблицу которого построить не удалось.</summary>
    public const string Failed = "Таблица раздела не построена — причина на вкладке Info";

    /// <summary>Панель свойств: у «Сводки» нет строк со свойствами.</summary>
    public const string Summary = "У «Сводки» нет свойств строк";
}
