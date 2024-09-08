using System.Text;

namespace Binary_Stream;

public static class Util
{
    /// <summary>
    /// Reads an item from an array of bytes
    /// </summary>
    public static T FromBytes<T>(ReadOnlySpan<byte> bytes, Endian endian, Encoding? enc = null) 
        where T : IRead, new()
    {
        using BinaryStream stream = new(bytes) {
            Endian = endian,
            Encoding = enc ?? Encoding.UTF8
        };

        return stream.ReadItem<T>();
    }

    /// <summary>
    /// Writes an item to an array of bytes
    /// </summary>
    public static byte[] ToBytes<T>(this T item, Endian endian, Encoding? enc = null)
        where T : IWrite
    {
        using BinaryStream stream = new() {
            Endian = endian,
            Encoding = enc ?? Encoding.UTF8
        };

        stream.WriteItem(item);
        return stream.ToArray();
    }
}