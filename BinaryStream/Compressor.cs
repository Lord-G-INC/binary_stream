using System.Numerics;

// Code inspired from yaz0-rs, which is under the MIT License.
// https://github.com/gcnhax/yaz0-rs/blob/master/src/deflate.rs


namespace Binary_Stream;

public abstract class CompressionLevel(int level = 7)
{
    protected int level = level;

    public int Level { get => Math.Clamp(level, 1, 10);
        set => level = value; }

    public static Naive Naive(int level) => new(level);
    public static Lookahead Lookahead(int level) => new(level);
}

public sealed class Naive(int level = 7) : CompressionLevel(level);

public sealed class Lookahead(int level = 7) : CompressionLevel(level);

class Run
{
    public int Cursor;
    public int Length;

    public static Run Zero() => new() { Cursor = 0, Length = 0 };

    public Run Swap(Run other)
    {
        if (Length > other.Length)
            return this;
        else
            return other;
    }
}

static class Compressor
{
    static Run FindNaiveRun(ReadOnlySpan<byte> src, int cursor,
        int lookback)
    {
        var search_start = cursor - lookback;
        var run = Run.Zero();
        for (int head = search_start; head <= cursor; head++)
        {
            var runlen = 0;
            while (runlen < src.Length - cursor)
            {
                if (src[head + runlen] != src[cursor + runlen])
                    break;
                runlen++;
            }

            run = run.Swap(new() { Cursor = head, Length = runlen });
        }
        return run;
    }

    static (bool, Run) FindLookaheadRun(ReadOnlySpan<byte> src, 
        int cursor, int lookback)
    {
        var run = FindNaiveRun(src, cursor, lookback);
        if (run.Length > 3)
        {
            var lookahead = FindNaiveRun(src, cursor + 1, lookback);
            if (lookahead.Length >= run.Length + 2)
                return (true, lookahead);
        }
        return (false, run);
    }

    // Hack to prevent int upcasting.
    static T OR<T>(this T left, T right) where T : IBitwiseOperators<T, T, T>
    {
        return left | right;
    }

    static void ORAssign<T>(ref T left, T right) where T : IBitwiseOperators<T, T, T>
    {
        left |= right;
    }

    static int WriteRun(int read_head, Run run, Stream destination)
    {
        var dist = read_head - run.Cursor - 1;
        if (run.Length > 0x12)
        {
            destination.WriteByte((byte)(dist >> 8));
            destination.WriteByte((byte)(dist & 0xff));
            var actual = Math.Min(run.Length, 0xff + 0x12);
            destination.WriteByte((byte)(actual - 0x12));
            return actual;
        } else
        {
            
            var data = ((byte)((run.Length - 2) << 4)).OR((byte)(dist >> 8));
            destination.WriteByte(data);
            destination.WriteByte((byte)(dist & 0xff));
            return run.Length;
        }
    }

    static byte[] CompressLookaround(ReadOnlySpan<byte> src, 
        CompressionLevel level)
    {
        var quality = level.Level;
        const int MAXLOOKBACK = 0x1000;
        var lookback = (int)MathF.Floor(MAXLOOKBACK / (10f / quality));
        Run? cache = null;
        var read_head = 0;
        // Alloc original source's length to prevent reallocs.

        using var encoded = new BinaryStream(src.Length);
        while (read_head < src.Length)
        {
            byte codon = 0;
            // 24 bytes is all we need

            using var packets = new BinaryStream(24);
            byte packet_n = 0;
            while (packet_n < 8)
            {
                (bool Hit, Run Best) tup = (false, Run.Zero());
                if (cache is not null)
                    tup = (false, cache);
                else
                {
                    if (level is Lookahead)
                        tup = FindLookaheadRun(src, read_head, lookback);
                    else if (level is Naive)
                        tup = (false, FindNaiveRun(src, read_head, lookback));
                }
                (bool hit, Run best) = tup;
                if (hit)
                    cache = best;
                if (best.Length >= 3 && !hit)
                    read_head += WriteRun(read_head, best, packets);
                else
                {
                    if (read_head >= src.Length)
                        break;
                    packets.WriteUInt8(src[read_head]);
                    ORAssign<byte>(ref codon, (byte)(0x80 >> packet_n));
                    read_head++;
                }
                packet_n++;
            }

            encoded.WriteUInt8(codon);
            encoded.Write(packets.ToArray());
        }

        return encoded.ToArray();
    }

    internal static byte[] Compress(ReadOnlySpan<byte> src, CompressionLevel level)
    {
        // Intentionally overallocate to prevent reallocation.

        using BinaryStream stream = new(src.Length, Endian.Big);
        stream.WriteString("Yaz0");
        stream.WriteUInt32((uint)src.Length);
        stream.Write([0, 0, 0, 0, 0, 0, 0, 0]);
        var compressed = CompressLookaround(src, level);
        stream.Write(compressed);
        return stream.ToArray();
    }
}