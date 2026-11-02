using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Объекты экранов с положением: элементы VIEW_* из списка объектов записей SCREEN (<see cref="ObjectBoundsReader"/>).
/// Сверено с TIA: 413 объектов A603A0097 (V21, Openness: 396 объектов 13 экранов и 17 объектов экранов 200–202),
/// Trend view (3) и f(x) trend view (2) — по Layout.
/// </summary>
public static class ScreenObjectParser
{
    /// <summary>Таблица записей экранов — для смещения в сообщении о таблице объекта, которой нет в TOC.</summary>
    private const string ScreenTable = "SCREEN";

    /// <summary>
    /// Обёртка объекта с анимацией видимости: u32, u32 R, R ссылок (условие — LIMIT), пара (u16 номер элемента, u16 Id
    /// таблицы) — сам объект, u32 (сверено: объекты внутри сошлись с TIA).
    /// </summary>
    private const string DarkTable = "VIEW_DARK";

    /// <summary>
    /// Контейнер: u32, u32 R, R ссылок, u16 (наблюдение: 0), u16 N, N пар — вложенные объекты. Так записаны группы TIA и
    /// Trend view с его рамкой (сверено с TIA).
    /// </summary>
    private const string MoveTable = "VIEW_MOVE";

    /// <summary>Смещение u16 числа пар VIEW_MOVE от конца ссылок: перед ним u16 (наблюдение: 0).</summary>
    private const int MoveCountOffset = 2;

    /// <summary>Смещение первой пары VIEW_MOVE от конца ссылок: за u16 и u16 числом пар.</summary>
    private const int MovePairsOffset = 4;

    /// <summary>Наибольшая вложенность обёрток; глубже — предупреждение (защита от зацикленных ссылок).</summary>
    private const int MaxDepth = 8;

    /// <summary>Число объектов VIEW_MOVE контрола с рамкой: сам контрол и объект без имени (<see cref="MergeFrame"/>).</summary>
    private const int ControlPairSize = 2;

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
        var reported = new HashSet<(VarReference Element, int? Width, int? Height)>();
        var failed = new HashSet<VarReference>();
        var collected = new Dictionary<VarReference, List<ObjectCandidate>>();
        foreach (var screen in doc.Screens.Where(s => !s.IsService))
        {
            var found = new List<ObjectCandidate>();
            foreach (var element in screen.Objects)
            {
                found.AddRange(Collected(doc, screen, element, collected, failed));
            }
            foreach (var candidate in found)
            {
                Add(doc, screen, candidate, reported);
            }
        }
    }

    /// <summary>
    /// Объекты элемента из списка экрана: прочитанные раньше (<paramref name="collected"/>) или прочитанные сейчас
    /// (<see cref="Collect"/>, сообщения — с этим экраном).
    /// </summary>
    /// <returns>Объекты элемента после <see cref="MergeFrame"/>; сбой — без объектов сбойного элемента.</returns>
    private static List<ObjectCandidate> Collected(
        FwxDocument doc,
        Screen screen,
        VarReference element,
        Dictionary<VarReference, List<ObjectCandidate>> collected,
        HashSet<VarReference> failed)
    {
        if (collected.TryGetValue(element, out var known))
        {
            return known;
        }
        var found = new List<ObjectCandidate>();
        Collect(doc, screen, element, 0, found, failed);
        collected[element] = found;
        return found;
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

    /// <summary>
    /// Объект по паре из списка экрана или обёртки: обёртка VIEW_DARK — объект в ней, VIEW_MOVE — вложенные объекты
    /// (<see cref="MergeFrame"/>), VIEW_DYN пропускается, остальное — объект. Сбой элемента — предупреждение (одно на
    /// элемент, <paramref name="failed"/>), объекта нет.
    /// </summary>
    /// <param name="doc">Документ панели.</param>
    /// <param name="screen">Экран — для сообщений.</param>
    /// <param name="element">Пара: Id таблицы и номер элемента.</param>
    /// <param name="depth">Вложенность обёрток.</param>
    /// <param name="found">Куда добавить объекты.</param>
    /// <param name="failed">Элементы, о сбое которых уже предупреждено.</param>
    private static void Collect(FwxDocument doc, Screen screen, VarReference element, int depth, List<ObjectCandidate> found, HashSet<VarReference> failed)
    {
        try
        {
            CollectElement(doc, screen, element, depth, found, failed);
        }
        catch (FwxFormatException e)
        {
            if (failed.Add(element))
            {
                doc.Warnings.Add(e.Message);
            }
        }
    }

    /// <summary>Как <see cref="Collect"/>, но сбой элемента — исключение.</summary>
    /// <exception cref="FwxFormatException">Таблицы нет в TOC, элемент вне таблицы, обёртка не сходится или вложенность больше 8.</exception>
    private static void CollectElement(FwxDocument doc, Screen screen, VarReference element, int depth, List<ObjectCandidate> found, HashSet<VarReference> failed)
    {
        var table = doc.FindTable(element.Table)
            ?? throw new FwxFormatException(ScreenTable, ScreenOffset(doc, screen), $"экран {screen.Name}: объект в таблице Id 0x{element.Table:X}, а её нет в TOC");
        if (table.Name == ViewDynParser.TableName)
        {
            return;
        }
        var item = ElementLink.Item(doc, table, element.Index, $"экран {screen.Name}");
        if (depth > MaxDepth)
        {
            throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: вложенность обёрток экрана {screen.Name} больше {MaxDepth}");
        }
        switch (table.Name)
        {
            case DarkTable:
                Collect(doc, screen, Inner(doc.Binary, table, item)[0], depth + 1, found, failed);
                break;
            case MoveTable:
                var inner = new List<ObjectCandidate>();
                foreach (var child in Inner(doc.Binary, table, item))
                {
                    Collect(doc, screen, child, depth + 1, inner, failed);
                }
                found.AddRange(MergeFrame(inner));
                break;
            default:
                found.Add(Candidate(doc.Binary, table, item, element));
                break;
        }
    }

    /// <summary>
    /// Смещение записи экрана в SCREEN (<see cref="FwxReader.Item"/> по <see cref="Screen.Index"/>); таблицы SCREEN нет
    /// или номер за её последним элементом — 0.
    /// </summary>
    /// <returns>Смещение записи от начала файла.</returns>
    /// <exception cref="FwxFormatException">Запись каталога смещений выходит за файл или даёт отрицательную длину.</exception>
    private static long ScreenOffset(FwxDocument doc, Screen screen) =>
        doc.FindTable(ScreenTable) is { } table && screen.Index < table.Entries
            ? FwxReader.Item(doc.Binary, table, screen.Index).Offset
            : 0;

    /// <summary>Объект: вид, имя и положение элемента (<see cref="ObjectBoundsReader"/>).</summary>
    /// <returns>Объект; положение не прочитано — с <c>null</c> вместо рамки.</returns>
    private static ObjectCandidate Candidate(FwxBinary b, TocEntry table, TableItem item, VarReference element)
    {
        var kind = NamedElement.Kind(b, item);
        return new ObjectCandidate(element, table.Name, item, kind, NamedElement.TryName(b, item) ?? "", ObjectBoundsReader.TryRead(b, table.Name, item, kind));
    }

    /// <summary>
    /// Контрол в VIEW_MOVE: ровно два объекта — элемент вида 0x0b и другой объект без имени с рамкой — рамка контрола
    /// (сверено с TIA на 3 Trend view Project1; так же записаны окна алармов, рецептов, диагностики и Status/Force — с
    /// TIA не сверены; сжатая рамка самого элемента, если читается, на 2 px меньше с каждой стороны). Иначе, в том числе
    /// у групп TIA, объекты остаются как есть.
    /// </summary>
    /// <returns>Объекты контейнера; у контрола — рамка соседнего объекта, сам соседний объект не выводится.</returns>
    private static List<ObjectCandidate> MergeFrame(List<ObjectCandidate> inner)
    {
        if (inner.Count != ControlPairSize)
        {
            return inner;
        }
        var controls = inner.Where(c => c.Kind == ObjectBoundsReader.ControlKind).ToList();
        var frames = inner.Where(c => c.Kind != ObjectBoundsReader.ControlKind && c.Name.Length == 0 && c.Bounds is not null).ToList();
        if (controls.Count != 1 || frames.Count != 1)
        {
            return inner;
        }
        return [controls[0] with { Bounds = frames[0].Bounds }];
    }

    /// <summary>
    /// Пары обёртки за её ссылками (<see cref="ObjectPairs"/>): у VIEW_DARK — одна сразу за ссылками, у VIEW_MOVE — u16,
    /// u16 N и N пар.
    /// </summary>
    /// <returns>Пары как ссылки: Id таблицы и номер элемента.</returns>
    /// <exception cref="FwxFormatException">Начало элемента не сходится, число или пары не помещаются.</exception>
    private static List<VarReference> Inner(FwxBinary b, TocEntry table, TableItem item)
    {
        var (end, _) = ReferenceHeader.Read(b, item, table.Name);
        if (table.Name != MoveTable)
        {
            return Pairs(b, table, item, end, 1);
        }
        return end + MovePairsOffset <= item.Length
            ? Pairs(b, table, item, end + MovePairsOffset, b.D2(item.Offset + end + MoveCountOffset))
            : throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: число объектов не помещается в {item.Length} байт");
    }

    /// <summary><paramref name="count"/> пар с <paramref name="first"/> от начала элемента (<see cref="ObjectPairs"/>).</summary>
    /// <returns>Пары как ссылки: Id таблицы и номер элемента.</returns>
    /// <exception cref="FwxFormatException">Пары не помещаются в элемент.</exception>
    private static List<VarReference> Pairs(FwxBinary b, TocEntry table, TableItem item, long first, int count)
    {
        return first + count * (long)ObjectPairs.Size <= item.Length
            ? ObjectPairs.Read(b, item.Offset + first, count)
            : throw new FwxFormatException(table.Name, item.Offset, $"запись {item.Index + 1}: {count} объектов не помещаются в {item.Length} байт");
    }

    /// <summary>Объект для сообщения: имя, у объекта без имени — таблица и номер элемента с единицы.</summary>
    /// <returns>Текст вида <c>Exit</c> или <c>VIEW_OUT 4</c>.</returns>
    private static string Title(ObjectCandidate candidate) =>
        candidate.Name.Length > 0 ? candidate.Name : $"{candidate.Table} {candidate.Item.Index + 1}";
}
