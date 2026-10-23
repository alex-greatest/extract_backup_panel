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

    /// <summary>Холст Permanent area: её высота 0 (VIEW_DYN), полосы на панели нет.</summary>
    public const string NoPermanentArea = "Permanent area нет: высота 0";

    /// <summary>Холст Permanent area: её высота не прочитана (на листе «Экраны» — <c>?</c>).</summary>
    public const string PermanentAreaHeightUnknown = "Высота Permanent area не прочитана";

    /// <summary>Панель свойств: открыта таблица, а строка не выбрана.</summary>
    public const string NoRow = "Выберите строку таблицы";

    /// <summary>Панель свойств: раздел, таблицу которого построить не удалось.</summary>
    public const string Failed = "Таблица раздела не построена — причина на вкладке Info";

    /// <summary>Панель свойств: открыты картинки, а картинка не выбрана.</summary>
    public const string NoPicture = "Выберите картинку";

    /// <summary>Панель свойств: у «Сводки» нет строк со свойствами.</summary>
    public const string Summary = "У «Сводки» нет свойств строк";
}
