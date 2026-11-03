using FlexRt.Model.Panel.Screens;

namespace FlexRt.Ui.Panel.Screens;

/// <summary>
/// Объект, выбранный щелчком на холсте экрана: холст его выделяет, панель свойств показывает его свойства вместо свойств
/// экрана. Выбор относится к одному экрану окна; щелчок по пустому месту экрана и смена экрана его снимают. Служба окна,
/// одна на окно (<see cref="MainWindow"/>): холст и панель свойств — соседние компоненты, о смене выбора панель узнаёт
/// по <see cref="Changed"/>.
/// </summary>
public sealed class ObjectSelection
{
    /// <summary>Экран, на котором выбран объект; ничего не выбрано — <c>null</c>.</summary>
    private ScreenItem? _screen;

    /// <summary>Выбранный объект; ничего не выбрано — <c>null</c>.</summary>
    private ScreenObject? _object;

    /// <summary>Выбор сменился: панель свойств перерисовывается.</summary>
    public event Action? Changed;

    /// <summary>Выбранный объект экрана <paramref name="screen"/>.</summary>
    /// <returns>Объект; на этом экране ничего не выбрано — <c>null</c>.</returns>
    public ScreenObject? Of(ScreenItem? screen) => screen is not null && ReferenceEquals(screen, _screen) ? _object : null;

    /// <summary>Выбрать объект на экране; <c>null</c> — снять выбор. Тот же выбор — ничего не происходит.</summary>
    public void Select(ScreenItem? screen, ScreenObject? selected)
    {
        var chosen = selected is null ? null : screen;
        if (ReferenceEquals(chosen, _screen) && Equals(selected, _object))
        {
            return;
        }
        (_screen, _object) = (chosen, selected);
        Changed?.Invoke();
    }
}
