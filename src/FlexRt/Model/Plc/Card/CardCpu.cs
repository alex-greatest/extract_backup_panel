namespace FlexRt.Model.Plc.Card;

/// <summary>CPU из конфигурации оборудования на карте ПЛК.</summary>
/// <param name="Name">Имя CPU в проекте TIA (<c>TM50</c>); <c>null</c>, если в объекте CPU нет имени.</param>
/// <param name="Model">Модель с заказным номером: <c>CPU 1515F-2 PN (6ES7 515-2FN03-0AB0)</c>.</param>
internal sealed record CardCpu(string? Name, string Model);
