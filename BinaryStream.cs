using System.Text;
using System.Runtime.CompilerServices;

namespace Binary_Stream;

/// <summary>
/// A stream of data with support for endianness, seeking, aligning and better string reading/writing.
/// </summary>
public class BinaryStream : MemoryStream {
    /// <summary>
    /// The native endianness of your computer architecture.
    /// </summary>
    public readonly static Endian Native = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;

    /// <summary>
    /// The endianness of this stream.
    /// </summary>
    public Endian Endian { get; set; } = Native;

    /// <summary>
    /// If the bytes should be reversed to get the requested endianness.
    /// </summary>
    public bool Reverse => Endian != Native;

    /// <summary>
    /// The default encoding for strings parsed from this stream.
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
    /// Reads an unmanaged type.
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
    /// Reads an unmanaged type into a variable.
    /// </summary>
    public BinaryStream ReadUnmanaged<T>(ref T value) where T : unmanaged {
        value = ReadUnmanaged<T>();
        return this;
    }

    /// <summary>
    /// Writes an unmanaged type.
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
    /// Writes multiple unmanaged values.
    /// </summary>
    public BinaryStream WriteUnmanaged<T>(params T[] values) where T : unmanaged
    {
        foreach (var value in values) {
            WriteUnmanaged(value);
        }

        return this;
    }

    /// <summary>
    /// Reads a string with the specified length and encoding.
    /// </summary>
    public string ReadString(int length, Encoding? enc = null) {
        byte[] data = new byte[length];
        Read(data);

        enc ??= Encoding;
        return enc.GetString(data);
    }

    /// <summary>
    /// Reads a null-terminated string.
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
    /// Reads a null-terminated string at a specific position.
    /// </summary>
    public string ReadNTStringAt(long offset, Encoding? enc = null) {
        return this.SeekTask(offset, x => x.ReadNTString(enc));
    }

    /// <summary>
    /// Reads a null-terminated string at a specific position.
    /// </summary>
    public string ReadNTStringAt(long offset, SeekOrigin origin, Encoding? enc = null) {
        return this.SeekTask(offset, origin, x => x.ReadNTString(enc));
    }

    /// <summary>
    /// Writes a string to the file with a specific encoding.
    /// </summary>
    public BinaryStream WriteString(string value, Encoding? enc = null) {
        enc ??= Encoding;
        Write(enc.GetBytes(value));

        return this;
    }

    /// <summary>
    /// Writes a null-terminated string with a specific encoding.
    /// </summary>
    public BinaryStream WriteNTString(string value, Encoding? enc = null) {
        WriteString(value, enc);
        WriteByte(0);
        return this;
    }

    /// <summary>
    /// Writes multiple null-terminated strings with a specific encoding.
    /// </summary>
    public BinaryStream WriteNTStrings(Encoding? enc, params string[] values) {
        WriteString(string.Join('\x00', values), enc);
        WriteByte(0);

        return this;
    }

    /// <summary>
    /// Reads a number of bytes from the current position.
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
    /// Reads a type that implements <see cref="IRead"/>.
    /// </summary>
    public T ReadItem<T>() where T : IRead, new() {
        T res = new();
        res.Read(this);
        return res;
    }

    /// <summary>
    /// Reads a type that implements <see cref="IRead"/> into a variable.
    /// </summary>
    public BinaryStream ReadItem<T>(ref T item) where T : IRead {
        item.Read(this);
        return this;
    }

    /// <summary>
    /// Writes a variable that implements <see cref="IWrite"/>.
    /// </summary>
    public BinaryStream WriteItem<T>(T item) where T : IWrite {
        item.Write(this);
        return this;
    }

    /// <summary>
    /// Aligns the cursor position to the specified value.
    /// </summary>
    public BinaryStream AlignTo(int alignment) {
        Seek(alignment - (Position % alignment), SeekOrigin.Current);
        return this;
    }

    /// <summary>
    /// Skips a number of bytes from the current position.
    /// </summary>
    public BinaryStream Skip(long count) {
        Seek(count, SeekOrigin.Current);
        return this;
    }

    public sbyte ReadInt8() { return ReadUnmanaged<sbyte>(); }
    public byte ReadUInt8() { return ReadUnmanaged<byte>(); }
    public short ReadInt16() { return ReadUnmanaged<short>(); }
    public ushort ReadUInt16() { return ReadUnmanaged<ushort>(); }
    public int ReadInt32() { return ReadUnmanaged<int>(); }
    public uint ReadUInt32() { return ReadUnmanaged<uint>(); }
    public long ReadInt64() { return ReadUnmanaged<long>(); }
    public ulong ReadUInt64() { return ReadUnmanaged<ulong>(); }

    public void WriteInt8(sbyte value) { WriteUnmanaged(value); }
    public void WriteUInt8(byte value) { WriteUnmanaged(value); }
    public void WriteInt16(short value) { WriteUnmanaged(value); }
    public void WriteUInt16(ushort value) { WriteUnmanaged(value); }
    public void WriteInt32(int value) { WriteUnmanaged(value); }
    public void WriteUInt32(uint value) { WriteUnmanaged(value); }
    public void WriteInt64(long value) { WriteUnmanaged(value); }
    public void WriteUInt64(ulong value) { WriteUnmanaged(value); }
}