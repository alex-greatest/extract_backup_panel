namespace FlexRt.Model.Panel.Screens;

/// <summary>Запись таблицы SCREEN: экран панели (обычный, pop-up, slide-in, Permanent area) или служебная запись pop-up.</summary>
/// <param name="Index">Номер записи в таблице, с нуля; на него ссылаются номера экранов (ADAPTVALUECHANG) и служебные записи.</param>
/// <param name="IsService">
/// Служебная запись (первое слово 1, имени нет): по одной на каждый pop-up экран, единственный объект — ссылка на
/// запись pop-up в SCREEN (наблюдение на Project1, A603A0097 и CCB-A13).
/// </param>
/// <param name="Name">Имя экрана, как в TIA (<c>Root screen</c>); у служебной записи — пустое.</param>
/// <param name="Width">Ширина в пикселях.</param>
/// <param name="Height">Высота в пикселях.</param>
/// <param name="Objects">Объекты экрана в порядке файла: <see cref="VarReference.Table"/> — Id таблицы объекта, <see cref="VarReference.Index"/> — номер элемента.</param>
public sealed record Screen(int Index, bool IsService, string Name, int Width, int Height, IReadOnlyList<VarReference> Objects);
