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
        using BinaryStream stream = new(item.TotalSize(), endian, enc);

        stream.WriteItem(item);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a byte span over <paramref name="item"/>. <paramref name="item"/> must be a unmanaged type.
    /// </summary>
    /// <typeparam name="T">The unmanaged type.</typeparam>
    /// <param name="item">The unmanaged item.</param>
    /// <returns>A byte span referencing the item. NOTE: Mutating the bytes WILL also update item. USE CAREFULLY.</returns>
    public static Span<byte> SpanFromRef<T>(scoped ref T item) where T : unmanaged
    {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref item, 1));
    }
}