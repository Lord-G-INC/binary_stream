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
    /// If the read bytes should be reversed to get the proper endianness.
    /// </summary>
    public bool Reverse => Endian != Native;

    /// <summary>
    /// The default encoding for strings parsed from this stream.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Creates a <see cref="BinaryStream"/>.
    /// </summary>
    public BinaryStream() : base() { }

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> with the specified endianness.
    /// </summary>
    /// <param name="endian">The endianness of this stream, will default to the <see cref="Native"/> endianness.</param>
    public BinaryStream(Endian endian) : base() {
        Endian = endian;
    }

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> with the specified capacity.
    /// </summary>
    /// <param name="capacity">The capacity of this stream.</param>
    /// <param name="endian">The endianness of this stream, will default to the <see cref="Native"/> endianness.</param>
    public BinaryStream(int capacity, Endian? endian = null) : base(capacity) {
        Endian = endian ?? Native;
    }

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> from a <see cref="ReadOnlySpan{byte}"/> of bytes.
    /// </summary>
    /// <param name="bytes">The bytes to copy to this stream.</param>
    /// <param name="endian">The endianness of this stream, will default to the <see cref="Native"/> endianness.</param>
    public BinaryStream(ReadOnlySpan<byte> bytes, Endian? endian = null) : this(bytes.Length) {
        Write(bytes);
        Position = 0;

        Endian = endian ?? Native;
    }

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> from an existing stream.
    /// </summary>
    /// <param name="sourceStream">The stream to copy the data from.</param>
    /// <param name="endian">The endianness of this stream, will default to the <see cref="Native"/> endianness.</param>
    public BinaryStream(Stream sourceStream, Endian? endian = null) : this((int)sourceStream.Length) {
        long oldpos = sourceStream.Position;

        sourceStream.Position = 0;
        sourceStream.CopyTo(this);
        sourceStream.Position = oldpos;

        Position = 0;

        Endian = endian ?? Native;
    }

    #region // ----- Unmanaged Reading/Writing ----- //

    /// <summary>
    /// Reads an unmanaged type from this stream.
    /// </summary>
    /// <remarks>
    /// Due to the nature of unmanaged reading works, structs might be read incorrectly.
    /// </remarks>
    public T ReadUnmanaged<T>() where T : unmanaged {
        Span<byte> bytes = new byte[Unsafe.SizeOf<T>()];

        Read(bytes);
        if (Reverse && bytes.Length > 1) {
            bytes.Reverse();
        }

        return Unsafe.ReadUnaligned<T>(ref bytes[0]);
    }

    /// <summary>
    /// Reads an unmanaged type and sets the value into a variable.
    /// </summary>
    /// <remarks>
    /// Due to the nature of unmanaged reading works, structs might be read incorrectly.
    /// </remarks>
    /// <param name="value">The variable to set the value to.</param>
    public BinaryStream ReadUnmanaged<T>(ref T value) where T : unmanaged {
        value = ReadUnmanaged<T>();
        return this;
    }

    /// <summary>
    /// Writes an unmanaged type to this stream.
    /// </summary>
    /// <remarks>
    /// Due to the nature of unmanaged writing works, structs might be writen incorrectly.
    /// </remarks>
    /// <param name="value">The value to be written.</param>
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
    /// Writes multiple unmanaged values to this stream.
    /// </summary>
    /// <param name="values">The values to be written.</param>
    public BinaryStream WriteUnmanaged<T>(params T[] values) where T : unmanaged {
        foreach (var value in values) {
            WriteUnmanaged(value);
        }

        return this;
    }

    /// <summary>
    /// Reads a number of bytes from the current position.
    /// </summary>
    public byte[] ReadBytes(int count) {
        if (count < 0) {
            throw new ArgumentOutOfRangeException(nameof(count), "Byte count cannot be negative.");
        }

        byte[] buffer = new byte[count];
        Read(buffer);

        return buffer;
    }

    #endregion
    #region // ----- String Reading/Writing ----- //

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

    #endregion
    #region // ----- Items Reading/Writing ----- //

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

    #endregion
    #region // ----- Positioning ----- //

    public Seek<BinaryStream> TemporarySeek(long offset, SeekOrigin origin = SeekOrigin.Begin)
        => new(this, offset, origin);

    /// <summary>
    /// Skips a number of bytes from the current position.
    /// </summary>
    public BinaryStream Skip(long count) {
        Seek(count, SeekOrigin.Current);
        return this;
    }

    /// <summary>
    /// Aligns the stream position to the specified <paramref name="alignment"/>.
    /// </summary>
    public BinaryStream AlignTo(int alignment) {
        Seek(alignment - (Position % alignment), SeekOrigin.Current);
        return this;
    }

    /// <summary>
    /// Writes <paramref name="value"/> until the stream position is aligned to <paramref name="alignment"/>.
    /// </summary>
    public BinaryStream WriteUntilAligned(int alignment, byte value) {
        var writeCount = alignment - (Position % alignment);

        for (long i = 0; i < writeCount; i++) {
            WriteByte(value);
        }

        return this;
    }


    #endregion
    #region // ----- Reading/Writing Utilities ----- //

    public bool ReadBool() { return ReadUnmanaged<bool>(); }
    public sbyte ReadInt8() { return ReadUnmanaged<sbyte>(); }
    public byte ReadUInt8() { return ReadUnmanaged<byte>(); }
    public short ReadInt16() { return ReadUnmanaged<short>(); }
    public ushort ReadUInt16() { return ReadUnmanaged<ushort>(); }
    public int ReadInt32() { return ReadUnmanaged<int>(); }
    public uint ReadUInt32() { return ReadUnmanaged<uint>(); }
    public long ReadInt64() { return ReadUnmanaged<long>(); }
    public ulong ReadUInt64() { return ReadUnmanaged<ulong>(); }
    public float ReadSingle() { return ReadUnmanaged<float>(); }
    public double ReadDouble() { return ReadUnmanaged<double>(); }

    public void WriteBool(bool value) { WriteUnmanaged(value); }
    public void WriteInt8(sbyte value) { WriteUnmanaged(value); }
    public void WriteUInt8(byte value) { WriteUnmanaged(value); }
    public void WriteInt16(short value) { WriteUnmanaged(value); }
    public void WriteUInt16(ushort value) { WriteUnmanaged(value); }
    public void WriteInt32(int value) { WriteUnmanaged(value); }
    public void WriteUInt32(uint value) { WriteUnmanaged(value); }
    public void WriteInt64(long value) { WriteUnmanaged(value); }
    public void WriteUInt64(ulong value) { WriteUnmanaged(value); }
    public void WriteSingle(float value) { WriteUnmanaged(value); }
    public void WriteDouble(double value) { WriteUnmanaged(value); }

    #endregion
}