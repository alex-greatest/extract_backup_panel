using System.Buffers.Binary;
using FlexRt.Binary;
using FlexRt.Model.Plc;

namespace FlexRt.Parsing.Plc;

/// <summary>Связи объекта PEData.plf с другими объектами.</summary>
internal static class PlfRelations
{
    /// <summary>С какого смещения объекта ищутся связи (наблюдение: до него лежит заголовок).</summary>
    private const int ScanStart = 0x30;

    /// <summary>Размер записи связи <c>[тип][класс цели][ID цели][0]</c>.</summary>
    private const int RecordSize = 0x10;

    /// <summary>Наибольшее значение типа связи (исключая).</summary>
    private const long MaxType = 0x4000000;

    /// <summary>
    /// Связи объекта: запись <c>[u32 тип][u32 класс][u32 ID][u32 0]</c> на любом смещении от
    /// <c>0x30</c>, у которой цель — другой живой объект, тип меньше 0x4000000, а старшие 16
    /// бит типа не нулевые. У DB, FB и корней интерфейса список связей не лежит в одном
    /// месте, поэтому ищется по всему объекту. Порядок — по смещению.
    /// </summary>
    /// <returns>Связи в порядке файла.</returns>
    /// <exception cref="FwxFormatException">Объект выходит за пределы файла.</exception>
    public static List<PlfRelation> Scan(PlfFile file, PlfObject obj)
    {
        var data = file.Binary.Span(obj.Offset, obj.Length);
        var result = new List<PlfRelation>();
        for (var p = ScanStart; p + RecordSize <= data.Length; p++)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(data[(p + 12)..]) != 0)
            {
                continue;
            }
            long type = BinaryPrimitives.ReadUInt32LittleEndian(data[p..]);
            long cls = BinaryPrimitives.ReadUInt32LittleEndian(data[(p + 4)..]);
            long id = BinaryPrimitives.ReadUInt32LittleEndian(data[(p + 8)..]);
            var isOther = cls != obj.Class || id != obj.Id;
            if (type is > 0 and < MaxType && type >> 16 != 0 && isOther && file.Contains(cls, id))
            {
                result.Add(new PlfRelation(type, cls, id));
            }
        }
        return result;
    }

    /// <summary>ID ПЛК, которому принадлежит объект: цель первой связи <see cref="PlfFormat.PlcRelation"/> на объект ПЛК.</summary>
    /// <returns>ID объекта ПЛК; 0, если такой связи нет.</returns>
    public static long PlcId(List<PlfRelation> relations)
    {
        return relations.FirstOrDefault(r => r is { Type: PlfFormat.PlcRelation, Class: PlfFormat.PlcClass }).Id;
    }

    /// <summary>Первая связь заданного типа на объект заданного класса.</summary>
    /// <returns>Связь; <c>default</c>, если такой нет.</returns>
    public static PlfRelation Find(List<PlfRelation> relations, long type, long cls)
    {
        return relations.FirstOrDefault(r => r.Type == type && r.Class == cls);
    }
}
