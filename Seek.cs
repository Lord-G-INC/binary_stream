namespace Binary_Stream;

/// <summary>
/// A class that can temporarily go to a position in a stream
/// </summary>
public class Seek<T> : IDisposable where T : Stream {
    protected T Stream;
    protected long OriginalPosition;

    public Seek(T stream, long offset, SeekOrigin origin = SeekOrigin.Begin) {
        Stream = stream;
        OriginalPosition = Stream.Position;
        Stream.Seek(offset, origin);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        Stream.Seek(OriginalPosition, SeekOrigin.Begin);
    }

    public void ExecTask(Action<T> func) {
        func(Stream);
    }

    public Res ExecTask<Res>(Func<T, Res> func) {
        return func(Stream);
    }
}

public static class SeekExt {
    public static void SeekTask<T>(this T stream, long offset, Action<T> func) where T : Stream {
        using var seek = new Seek<T>(stream, offset);
        seek.ExecTask(func);
    }
    public static void SeekTask<T>(this T stream, long offset, SeekOrigin origin, Action<T> func) where T : Stream {
        using var seek = new Seek<T>(stream, offset, origin);
        seek.ExecTask(func);
    }
    public static Ret SeekTask<T, Ret>(this T stream, long offset, Func<T, Ret> func) where T : Stream {
        using var seek = new Seek<T>(stream, offset);
        return seek.ExecTask(func);
    }
    public static Ret SeekTask<T, Ret>(this T stream, long offset, SeekOrigin origin, Func<T, Ret> func) where T : Stream {
        using var seek = new Seek<T>(stream, offset, origin);
        return seek.ExecTask(func);
    }
}