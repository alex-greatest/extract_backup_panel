namespace FlexRt.Model.Panel.Screens;

/// <summary>Оформление одного состояния кнопки; отсутствующий или непрочитанный компонент — null.</summary>
/// <param name="Border">Рамка и радиус углов.</param>
/// <param name="Text">Цвет и расположение текста.</param>
/// <param name="Fill">Основной цвет заливки и прозрачность.</param>
/// <param name="FirstGradient">Первый скомпилированный переход градиента.</param>
/// <param name="SecondGradient">Второй скомпилированный переход градиента.</param>
/// <param name="Frame">Картинка оформления и границы её растягиваемой области.</param>
/// <param name="Picture">Поля и выравнивание графической подписи.</param>
public sealed record ButtonStateAppearance(
    BorderStyle? Border,
    TextStyle? Text,
    ButtonFill? Fill,
    ButtonGradient? FirstGradient,
    ButtonGradient? SecondGradient,
    ButtonFrame? Frame,
    ButtonPictureLayout? Picture);
