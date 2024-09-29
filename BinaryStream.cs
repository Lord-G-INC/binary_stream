using System.Text;
using System.Runtime.CompilerServices;

namespace Binary_Stream;

/// <summary>
/// A stream of data with support for endianness and better string IO.
/// </summary>
public class BinaryStream : MemoryStream {
    /// <summary>
    /// The native endianness type
    /// </summary>
    public readonly static Endian Native = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;

    /// <summary>
    /// The endianness type of this stream
    /// </summary>
    public Endian Endian { get; set; } = Native;

    /// <summary>
    /// If when writing values, should the be reversed
    /// </summary>
    public bool Reverse => Endian != Native;

    /// <summary>
    /// The encoding type for strings in this stream
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    public BinaryStream() : base() {}

    public BinaryStream(int capacity) : base(capacity) {}

    public BinaryStream(ReadOnlySpan<byte> bytes) : this(bytes.Length) {
        Write(bytes);
        Position = 0;
    }

    public BinaryStream(Stream sourceStream) : this((int)sourceStream.Length) {
        long oldpos = sourceStream.Position;

        sourceStream.Position = 0;
        sourceStream.CopyTo(this);
        sourceStream.Position = oldpos;

        Position = 0;
    }

    /// <summary>
    /// Reads an unmanaged type
    /// </summary>
    public T ReadUnmanaged<T>() where T : unmanaged {
        Span<byte> bytes = new byte[Unsafe.SizeOf<T>()];

        Read(bytes);
        if (Reverse && bytes.Length > 1) {
            bytes.Reverse();
        }

        return Unsafe.ReadUnaligned<T>(ref bytes[0]);
    }

    /// <summary>
    /// Reads to a variable with an unmanaged type
    /// </summary>
    public BinaryStream ReadUnmanaged<T>(ref T value) where T : unmanaged {
        value = ReadUnmanaged<T>();
        return this;
    }

    /// <summary>
    /// Writes an unmanaged type
    /// </summary>
    public BinaryStream WriteUnmanaged<T>(T value) where T : unmanaged {
        Span<byte> bytes = new byte[Unsafe.SizeOf<T>()];
        Unsafe.WriteUnaligned(ref bytes[0], value);

        if (Reverse && bytes.Length > 1) {
            bytes.Reverse();
        }
            
        Write(bytes);
        return this;
    }

    /// <summary>
    /// Writes multiple unmanaged values
    /// </summary>
    public BinaryStream WriteUnmanaged<T>(params T[] values) where T : unmanaged
    {
        foreach (var value in values) {
            WriteUnmanaged(value);
        }

        return this;
    }

    /// <summary>
    /// Reads a string with the specified length and encoding
    /// </summary>
    public string ReadString(int length, Encoding? enc = null) {
        byte[] data = new byte[length];
        Read(data);

        enc ??= Encoding;
        return enc.GetString(data);
    }

    /// <summary>
    /// Reads a null (0x00) terminated string
    /// </summary>
    public string ReadNTString(Encoding? enc = null, byte capacity = 127) {
        List<byte> bytes = new(capacity);

        for (byte b = (byte)ReadByte(); b != 0x00; b = (byte)ReadByte()) {
            bytes.Add(b);
        }

        enc ??= Encoding;

        return enc.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Reads a null (0x00) terminated string at a specific position
    /// </summary>
    public string ReadNTStringAt(long pos, Encoding? enc = null) {
        return this.SeekTask(pos, x => x.ReadNTString(enc));
    }

    /// <summary>
    /// Reads a null (0x00) terminated string at a specific position
    /// </summary>
    public string ReadNTStringAt(long pos, SeekOrigin origin, Encoding? enc = null) {
        return this.SeekTask(pos, origin, x => x.ReadNTString(enc));
    }

    /// <summary>
    /// Writes a string
    /// </summary>
    public BinaryStream WriteString(string value, Encoding? enc = null) {
        enc ??= Encoding;
        Write(enc.GetBytes(value));

        return this;
    }

    /// <summary>
    /// Writes a null (0x00) terminated string
    /// </summary>
    public BinaryStream WriteNTString(string value, Encoding? enc = null) {
        WriteString(value, enc);
        WriteByte(0);
        return this;
    }

    /// <summary>
    /// Writes multiple null (0x00) terminated strings
    /// </summary>
    public BinaryStream WriteNTStrings(Encoding? enc, params string[] values) {
        WriteString(string.Join('\x00', values), enc);
        WriteByte(0);

        return this;
    }

    /// <summary>
    /// Reads a number of bytes from the current position
    /// </summary>
    public byte[] ReadBytes(int count) {
        if (count < 0) {
            throw new ArgumentOutOfRangeException(nameof(count), "Byte count cannot be negative.");
        }

        long bytesToEnd = Length - Position;
        if (count > bytesToEnd) {
            throw new ArgumentOutOfRangeException(nameof(count), $"Byte count overflow, attempted to read {count} bytes with only {bytesToEnd} available.");
        }

        byte[] buffer = new byte[count];

        Read(buffer);

        return buffer;
    }

    /// <summary>
    /// Reads a type that implements IRead
    /// </summary>
    public T ReadItem<T>() where T : IRead, new() {
        T res = new();
        res.Read(this);
        return res;
    }

    /// <summary>
    /// Reads to a variable that implements IRead
    /// </summary>
    public BinaryStream ReadItem<T>(ref T item) where T : IRead {
        item.Read(this);
        return this;
    }

    /// <summary>
    /// Writes a variable that implements IRead
    /// </summary>
    public BinaryStream WriteItem<T>(T item) where T : IWrite {
        item.Write(this);
        return this;
    }

    /// <summary>
    /// Aligns the cursor position to the specified value
    /// </summary>
    public BinaryStream AlignTo(int alignment) {
        Seek(alignment - (Position % alignment), SeekOrigin.Current);
        return this;
    }

    /// <summary>
    /// Skips a number of bytes from the current position
    /// </summary>
    public BinaryStream Skip(long count) {
        Seek(count, SeekOrigin.Current);
        return this;
    }
}