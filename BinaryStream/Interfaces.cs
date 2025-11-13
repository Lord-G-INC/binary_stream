using System.Text;

namespace Binary_Stream;

/// <summary>
/// A interface for types that can read data from a <see cref="BinaryStream"/>.
/// </summary>
public interface IRead
{
    /// <summary>
    /// Reads data from a <see cref="BinaryStream"/>.
    /// </summary>
    /// <param name="stream">The stream.</param>
    public void Read(BinaryStream stream);
}

/// <summary>
/// A interface for types that can write data to a <see cref="BinaryStream"/>.
/// </summary>
public interface IWrite
{
    /// <summary>
    /// Calculates the total size this type uses.
    /// Note: Only implement this if you expect your type may be used by
    /// <seealso cref="Util.ToBytes{T}(T, Endian?, Encoding?)"/>.
    /// </summary>
    /// <returns></returns>
    public virtual int TotalSize() => 0;
    /// <summary>
    /// Write the data of this type to the <see cref="BinaryStream"/>.
    /// </summary>
    /// <param name="stream">The stream.</param>
    public void Write(BinaryStream stream);
}

public interface ILoadable<T> where T : ILoadable<T> {
    public abstract static T LoadFrom(BinaryStream stream, Endian? endian = null, Encoding? enc = null);
}

public interface ILoadTo {
    public T LoadTo<T>(Endian? endian = null, Encoding? enc = null) where T : ILoadable<T>;
}

public class Loader<T>(T item) : ILoadable<Loader<T>> where T : IRead, new()
{
    readonly T item = item;

    public static Loader<T> LoadFrom(BinaryStream stream, Endian? endian = null, Encoding? enc = null)
    {
        stream.Endian = endian ?? stream.Endian;
        stream.Encoding = enc ?? stream.Encoding;
        return new(stream.ReadItem<T>());
    }
    public static implicit operator T(Loader<T> self) => self.item;
    public static implicit operator Loader<T>(T self) => new(self);
}

public interface IMagic
{
    public abstract static string BE_MAGIC { get; }
    public abstract static string LE_MAGIC { get; }
    public Endian Endian { get; set; }
}

public static class MagicExt
{
    public static void SetEndian<T>(this T item, BinaryStream stream, Encoding? enc = null)
        where T : IMagic
    {
        string str = stream.ReadString(T.BE_MAGIC.Length, enc);
        Endian endian;
        if (str == T.BE_MAGIC)
            endian = Endian.Big;
        else if (str == T.LE_MAGIC)
            endian = Endian.Little;
        else
            endian = BinaryStream.Native;
        item.Endian = endian;
    }
}