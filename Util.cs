namespace binary_stream;

public static class Util
{
    public static T FromBytes<T>(ReadOnlySpan<byte> bytes, Endian endian, Encoding? enc = null) 
        where T : IRead, new()
    {
        using BinaryStream stream = new(bytes) { Endian = endian, Encoding = enc ?? Encoding.UTF8 };
        return stream.ReadItem<T>();
    }

    public static byte[] IntoBytes<T>(this T item, Endian endian, Encoding? enc = null)
        where T : IWrite
    {
        using BinaryStream stream = new() { Endian = endian, Encoding = enc ?? Encoding.UTF8 };
        stream.WriteItem(item);
        return stream.ToArray();
    }
}