namespace FlexRt.Model.Panel.Screens;

/// <summary>
/// Анимация Visibility экрана — элемент таблицы LIMIT, на который ссылается запись SCREEN (сверено с TIA на шагах
/// <c>data/real/steps-animations/</c> 01–08, Project1, V21).
/// </summary>
/// <param name="ScreenIndex">Номер записи SCREEN экрана, с нуля.</param>
/// <param name="Tag">Ссылка на тег: <see cref="VarReference.Table"/> — Id таблицы (у всех известных — VAR), <see cref="VarReference.Index"/> — номер записи с нуля.</param>
/// <param name="Visible">Visible в TIA (<c>true</c>) или Invisible (<c>false</c>).</param>
/// <param name="From">Начало диапазона Range; <c>null</c> — тип значения не известен.</param>
/// <param name="To">Конец диапазона Range; <c>null</c> — тип значения не известен.</param>
public sealed record ScreenVisibility(int ScreenIndex, VarReference Tag, bool Visible, double? From, double? To);
