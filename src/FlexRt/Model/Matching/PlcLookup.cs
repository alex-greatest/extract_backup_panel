namespace FlexRt.Model.Matching;

/// <summary>Чем закончился поиск тега панели в проекте ПЛК.</summary>
public enum PlcLookup
{
    /// <summary>Тег найден.</summary>
    Found,
    /// <summary>ПЛК выбран, но тега в нём нет или он другого типа.</summary>
    NotInPlcFile,
    /// <summary>
    /// Тег с абсолютным доступом, ПЛК выбран, а тега ПЛК с этим адресом нет: HMI-тег ссылается
    /// прямо на адрес, без символа. В TIA так можно, это не ошибка.
    /// </summary>
    AddressOnly,
    /// <summary>Данных нет: файла ПЛК нет, он сломан, ПЛК не выбран или тег не разобран.</summary>
    Unknown
}
