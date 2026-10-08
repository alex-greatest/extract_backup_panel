using FlexRt.Binary;
using FlexRt.Model.Plc.Plf;

namespace FlexRt.Parsing.Plc.Plf;

/// <summary>Прочитанный PEData.plf: байты файла и актуальные объекты по ключу (класс, ID).</summary>
/// <param name="binary">Байты файла; <see cref="FwxBinary.Section"/> уже выставлена.</param>
/// <param name="objects">Актуальные объекты проекта.</param>
internal sealed class PlfFile(FwxBinary binary, Dictionary<(long, long), PlfObject> objects)
{
    /// <summary>Байты файла.</summary>
    public FwxBinary Binary { get; } = binary;

    /// <summary>
    /// Объекты класса <paramref name="cls"/> в порядке первого появления объекта в файле (не
    /// его последней версии).
    /// </summary>
    /// <returns>Объекты класса.</returns>
    public IEnumerable<PlfObject> OfClass(long cls) => objects.Values.Where(o => o.Class == cls);

    /// <summary>Объекты класса <paramref name="cls"/> по возрастанию ID.</summary>
    /// <returns>Объекты класса.</returns>
    public IEnumerable<PlfObject> OfClassById(long cls) => OfClass(cls).OrderBy(o => o.Id);

    /// <summary>Объект по классу и ID; он есть, потому что связи берутся только на живые объекты.</summary>
    /// <returns>Объект.</returns>
    public PlfObject Get(long cls, long id) => objects[(cls, id)];

    /// <summary>Объект по классу и ID.</summary>
    /// <returns><c>true</c>, если объект есть.</returns>
    public bool TryGet(long cls, long id, out PlfObject obj) => objects.TryGetValue((cls, id), out obj);

    /// <summary>Есть ли живой объект с такими классом и ID.</summary>
    /// <returns><c>true</c>, если объект есть.</returns>
    public bool Contains(long cls, long id) => objects.ContainsKey((cls, id));
}
