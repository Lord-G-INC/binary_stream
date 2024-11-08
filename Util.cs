using System.Runtime.InteropServices;
using System.Text;

namespace Binary_Stream;

public static class Util
{
    /// <summary>
    /// Reads an item from an array of bytes.
    /// </summary>
    public static T FromBytes<T>(ReadOnlySpan<byte> bytes, Endian? endian = null, Encoding? enc = null) 
        where T : IRead, new()
    {
        using BinaryStream stream = new(bytes, endian, enc);

        return stream.ReadItem<T>();
    }

    /// <summary>
    /// Writes an item to an array of bytes.
    /// </summary>
    public static byte[] ToBytes<T>(this T item, Endian? endian = null, Encoding? enc = null)
        where T : IWrite
    {
        using BinaryStream stream = new(endian, enc);

        stream.WriteItem(item);
        return stream.ToArray();
    }

    public static Span<byte> SpanFromRef<T>(scoped ref T item) where T : unmanaged {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref item, 1));
    }
}