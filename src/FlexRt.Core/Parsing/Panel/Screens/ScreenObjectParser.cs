using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;
using FlexRt.Parsing.Panel.Screens.Objects;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Объекты экранов с положением: элементы VIEW_* из списка объектов записей SCREEN (<see cref="ObjectBoundsReader"/>).
/// Сверено с TIA: 413 объектов A603A0097 (V21, Openness: 396 объектов 13 экранов и 17 объектов экранов 200–202),
/// Trend view (3) и f(x) trend view (2) — по Layout.
/// </summary>
public static class ScreenObjectParser
{
    /// <summary>
    /// Прочитать объекты всех экранов (кроме служебных записей pop-up) в <see cref="FwxDocument.ScreenObjects"/>.
    /// Разбирается после SCREEN. Объект без прочитанного положения не добавляется; объект за пределами экрана
    /// добавляется; в обоих случаях и при сбое элемента — предупреждение с таблицей и смещением (решение пользователя
    /// 2026-11-02). Элемент из списка экрана читается один раз (объект шаблона — с первым экраном), его объекты
    /// входят в каждый экран. Предупреждение «положение не прочитано» и о сбое элемента — одно на элемент, о рамке за
    /// экраном — одно на элемент и размер экрана.
    /// </summary>
    public static void Parse(FwxDocument doc)
    {
        var collector = new ScreenObjectCollector(doc);
        var reported = new HashSet<(VarReference Element, int? Width, int? Height)>();
        foreach (var screen in doc.Screens.Where(screen => !screen.IsService))
        {
            // Предупреждения при чтении списка предшествуют предупреждениям о положении.
            var found = screen.Objects.SelectMany(element => collector.Read(screen, element)).ToList();
            foreach (var candidate in found)
            {
                Add(doc, screen, candidate, reported);
            }
        }
    }

    /// <summary>
    /// Добавить объект в документ или предупредить, что положение не прочитано; рамка вне экрана — предупреждение.
    /// Предупреждение о положении — один раз на элемент, о рамке вне экрана — на элемент и размер экрана
    /// (<paramref name="reported"/>).
    /// </summary>
    private static void Add(FwxDocument doc, Screen screen, ObjectCandidate candidate, HashSet<(VarReference, int?, int?)> reported)
    {
        if (candidate.Bounds is { } r)
        {
            doc.ScreenObjects.Add(new ScreenObject(screen.Index, candidate.Element, candidate.Table, candidate.Name, r));
        }
        var key = candidate.Bounds is null
            ? (candidate.Element, (int?)null, (int?)null)
            : (candidate.Element, screen.Width, screen.Height);
        if ((candidate.Bounds is { } shown && Inside(shown, screen)) || !reported.Add(key))
        {
            return;
        }
        var problem = candidate.Bounds is { } b
            ? $"рамка {b.Left}, {b.Top}, {b.Width}×{b.Height} выходит за экран {screen.Width}×{screen.Height}"
            : "положение не прочитано";
        var who = $"запись {candidate.Item.Index + 1}: объект {Title(candidate)} экрана {screen.Name}";
        doc.Warnings.Add(new FwxFormatException(candidate.Table, candidate.Item.Offset, $"{who}: {problem}").Message);
    }

    /// <summary>Рамка целиком на экране: не отрицательная и не выходит за ширину и высоту записи.</summary>
    /// <returns><c>true</c> — на экране.</returns>
    private static bool Inside(ScreenRect r, Screen screen) =>
        r is { Width: >= 0, Height: >= 0, Left: >= 0, Top: >= 0 } && r.Left + r.Width <= screen.Width && r.Top + r.Height <= screen.Height;

    /// <summary>Объект для сообщения: имя, у объекта без имени — таблица и номер элемента с единицы.</summary>
    /// <returns>Текст вида <c>Exit</c> или <c>VIEW_OUT 4</c>.</returns>
    private static string Title(ObjectCandidate candidate) =>
        candidate.Name.Length > 0 ? candidate.Name : $"{candidate.Table} {candidate.Item.Index + 1}";
}
