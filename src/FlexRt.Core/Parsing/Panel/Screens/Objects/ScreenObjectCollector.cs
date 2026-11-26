using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens.Objects;

/// <summary>Обход элементов и обёрток экрана с кэшем прямых ссылок из SCREEN.</summary>
internal sealed class ScreenObjectCollector
{
    /// <summary>Таблица экранов для сообщения о таблице объекта, которой нет в TOC.</summary>
    private const string ScreenTable = "SCREEN";

    /// <summary>Число дочерних объектов у контрола с отдельной рамкой.</summary>
    private const int ControlPairSize = 2;

    private readonly FwxDocument _doc;
    private readonly Dictionary<VarReference, List<ObjectCandidate>> _collected = [];
    private readonly HashSet<VarReference> _failed = [];

    /// <summary>Начать обход экранов документа.</summary>
    public ScreenObjectCollector(FwxDocument doc)
    {
        _doc = doc;
    }

    /// <summary>Прочитать прямую ссылку из SCREEN или взять результат её первого чтения.</summary>
    /// <returns>Объекты со считанным или неизвестным положением; повреждённые элементы пропущены.</returns>
    public IReadOnlyList<ObjectCandidate> Read(Screen screen, VarReference element)
    {
        if (_collected.TryGetValue(element, out var found))
        {
            return found;
        }
        found = [];
        Collect(screen, element, 0, found);
        _collected[element] = found;
        return found;
    }

    /// <summary>Собрать объекты ссылки; при сбое предупредить один раз, указав экран первого сбоя.</summary>
    private void Collect(Screen screen, VarReference element, int depth, List<ObjectCandidate> found)
    {
        try
        {
            CollectElement(screen, element, depth, found);
        }
        catch (FwxFormatException error)
        {
            if (_failed.Add(element))
            {
                _doc.Warnings.Add(error.Message);
            }
        }
    }

    /// <summary>Развернуть дочерние объекты обёртки или прочитать положение элемента; VIEW_DYN пропускается.</summary>
    /// <exception cref="FwxFormatException">Таблицы нет в TOC, элемент вне таблицы, обёртка повреждена или слишком глубоко вложена.</exception>
    private void CollectElement(Screen screen, VarReference element, int depth, List<ObjectCandidate> found)
    {
        var table = _doc.FindTable(element.Table)
            ?? throw new FwxFormatException(ScreenTable, ScreenOffset(screen),
                $"экран {screen.Name}: объект в таблице Id 0x{element.Table:X}, а её нет в TOC");
        if (table.Name == ViewDynParser.TableName)
        {
            return;
        }
        var item = ElementLink.Item(_doc, table, element.Index, $"экран {screen.Name}");
        if (depth > ObjectWrapperReader.MaxDepth)
        {
            throw new FwxFormatException(table.Name, item.Offset,
                $"запись {item.Index + 1}: вложенность обёрток экрана {screen.Name} больше {ObjectWrapperReader.MaxDepth}");
        }
        if (table.Name is not (ObjectWrapperReader.DarkTable or ObjectWrapperReader.MoveTable))
        {
            var kind = NamedElement.Kind(_doc.Binary, item);
            var name = NamedElement.TryName(_doc.Binary, item) ?? "";
            var bounds = ObjectBoundsReader.TryRead(_doc.Binary, table.Name, item, kind);
            found.Add(new ObjectCandidate(element, table.Name, item, kind, name, bounds));
            return;
        }

        var (end, _) = ReferenceHeader.Read(_doc.Binary, item, table.Name);
        var children = ObjectWrapperReader.Read(_doc.Binary, table, item, end);
        if (table.Name == ObjectWrapperReader.DarkTable)
        {
            Collect(screen, children[0], depth + 1, found);
            return;
        }
        var inner = new List<ObjectCandidate>();
        foreach (var child in children)
        {
            Collect(screen, child, depth + 1, inner);
        }
        found.AddRange(MergeFrame(inner));
    }

    /// <summary>Получить смещение записи для предупреждения об объекте, таблицы которого нет в TOC.</summary>
    /// <returns>Смещение от начала файла; записи нет — 0.</returns>
    /// <exception cref="FwxFormatException">Запись каталога смещений выходит за файл или даёт отрицательную длину.</exception>
    private long ScreenOffset(Screen screen) =>
        _doc.FindTable(ScreenTable) is { } table && screen.Index < table.Entries
            ? FwxReader.Item(_doc.Binary, table, screen.Index).Offset
            : 0;

    /// <summary>
    /// Рамка контрола в VIEW_MOVE берётся у единственного соседнего объекта без имени, если всего объектов два.
    /// Подтверждено на трёх Trend view Project1. Так же записаны окна алармов, рецептов, диагностики и Status/Force
    /// (с TIA не сверены). Сжатая рамка самого контрола (если читается) на 2 px меньше с каждой стороны.
    /// </summary>
    /// <returns>Контрол с рамкой соседа или исходные дочерние объекты в их порядке.</returns>
    private static List<ObjectCandidate> MergeFrame(List<ObjectCandidate> inner)
    {
        if (inner.Count != ControlPairSize)
        {
            return inner;
        }
        var control = inner[0];
        var frame = inner[1];
        if (frame.Kind == ObjectBoundsReader.ControlKind)
        {
            (control, frame) = (frame, control);
        }
        return control.Kind == ObjectBoundsReader.ControlKind && frame.Kind != ObjectBoundsReader.ControlKind
            && frame.Name.Length == 0 && frame.Bounds is not null
            ? [control with { Bounds = frame.Bounds }]
            : inner;
    }
}
