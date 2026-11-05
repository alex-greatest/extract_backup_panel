using System.Buffers.Binary;
using FlexRt.Binary;

namespace FlexRt.Parsing.Panel;

/// <summary>Внешнее XOR-преобразование PWX/PWL из FwSecSys.dll; проверено на Project1, CCB-A13 и CSM1_CP1.</summary>
internal static class UserFileCipher
{
    /// <summary>Заголовок из двух состояний генератора, по четыре байта.</summary>
    private const int SeedSize = 8;

    /// <summary>Снять XOR, сохранив исходные смещения: первые восемь байт остаются на месте.</summary>
    /// <returns>Расшифрованные байты.</returns>
    /// <exception cref="FwxFormatException">Нет двух состояний или длина данных не кратна слову.</exception>
    public static byte[] Decode(byte[] bytes)
    {
        if (bytes.Length < SeedSize || (bytes.Length - SeedSize) % sizeof(uint) != 0)
        {
            throw new FwxFormatException("PWL", 0, "нет заголовка из 8 байт или длина данных не кратна 4");
        }
        var decoded = (byte[])bytes.Clone();
        long first = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        long second = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sizeof(uint)));
        for (var offset = SeedSize; offset < bytes.Length; offset += sizeof(uint))
        {
            // Константы двух генераторов и разности — из CXorCrypt::Crypt в FwSecSys.dll.
            first = first * 40014 % 2147483563;
            second = second * 40692 % 2147483399;
            var key = first - second;
            if (key < 1)
            {
                key += 2147483562;
            }
            var word = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
            BinaryPrimitives.WriteUInt32LittleEndian(decoded.AsSpan(offset), word ^ (uint)key);
        }
        return decoded;
    }
}
