using System.Text;

namespace Binary_Stream;

public interface IRead {
    public void Read(BinaryStream stream);
}

public interface IWrite {
    public void Write(BinaryStream stream);
}

public interface ILoadable<T> where T : ILoadable<T> {
    public abstract static T LoadFrom(BinaryStream stream, Endian? endian = null, Encoding? enc = null);
}

public interface ILoadTo {
    public T LoadTo<T>(Endian? endian = null, Encoding? enc = null) where T : ILoadable<T>;
}

public class Loader<T>(T item) : ILoadable<Loader<T>> where T : IRead, new() {
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