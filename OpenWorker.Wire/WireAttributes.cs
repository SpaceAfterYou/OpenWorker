namespace OpenWorker.Wire;

/// <summary>
/// Which methods the generator writes for a <see cref="WireMessageAttribute"/> struct.
/// </summary>
[Flags]
public enum WireMethods
{
    /// <summary>
    /// A request is read, a response is written, any other value both.
    /// </summary>
    Inferred = 0,
    Read = 1,
    Write = 2,
    Both = Read | Write
}

/// <summary>
/// The prefix that carries the count of a list.
/// </summary>
public enum WireLength
{
    /// <summary>
    /// <see langword="byte"/>.
    /// </summary>
    Byte,

    /// <summary>
    /// <see langword="short"/>.
    /// </summary>
    Signed16,

    /// <summary>
    /// <see langword="ushort"/>.
    /// </summary>
    Unsigned16,

    /// <summary>
    /// <see langword="int"/>.
    /// </summary>
    Signed32
}

/// <summary>
/// How a string goes on the wire. The names are the ones of the helpers of <c>BinaryWriter</c> and <c>BinaryReader</c>, and
/// so are the layouts: "Utf8" and "Utf16" say how the 16-bit length is counted, "Ascii" and "Unicode" how the characters are
/// encoded (one byte or two little endian), and a terminator is one zero byte.
/// </summary>
public enum WireStringFormat
{
    /// <summary>
    /// No length: the characters in ASCII and a zero byte.
    /// </summary>
    Ascii,

    /// <summary>
    /// No length and no terminator: the characters in ASCII, as many as <see cref="WireStringAttribute.Length"/> when read.
    /// </summary>
    AsciiWithoutTerminator,

    /// <summary>
    /// Length = characters + 1, ASCII, a zero byte.
    /// </summary>
    Utf8Ascii,

    /// <summary>
    /// Length = 2 × characters + 1, ASCII, a zero byte.
    /// </summary>
    Utf16Ascii,

    /// <summary>
    /// Length = characters, ASCII.
    /// </summary>
    Utf8AsciiWithoutTerminator,

    /// <summary>
    /// Written: length = characters + 1, UTF-16, a zero byte. Read: the length in bytes, the terminator included.
    /// </summary>
    Utf8Unicode,

    /// <summary>
    /// Written: length = 2 × characters + 1, UTF-16, a zero byte. Read: the length in characters, a two-byte terminator
    /// included.
    /// </summary>
    Utf16Unicode,

    /// <summary>
    /// Written: length = characters, UTF-16. Read: the length in bytes.
    /// </summary>
    Utf8UnicodeWithoutTerminator,

    /// <summary>
    /// Length = characters, UTF-16.
    /// </summary>
    Utf16UnicodeWithoutTerminator
}

/// <summary>
/// The generator writes <c>Read(ref PacketReader)</c> and <c>Write(ref PacketWriter)</c> for this <see langword="partial"/>
/// struct, and the bridges to <c>BinaryReader</c> and <c>BinaryWriter</c> (a constructor and <c>Write(BinaryWriter)</c>) for
/// code that still reads or writes through them. The fields are the public instance auto-properties, in the order they are
/// declared, which is the order on the wire; a computed property (<c>Opcode =&gt; ...</c>) is not a field.<br/>
/// The type of a field says how it goes: the primitives, <see langword="bool"/> (one byte), an enum by its underlying type,
/// <c>Vector3</c> (three floats), <see cref="DateTime"/> and <see cref="DateTimeOffset"/> (64-bit seconds since 1970),
/// <see cref="TimeSpan"/> (32-bit whole seconds), a value that implements
/// <see cref="IWireReadable{TSelf}"/> and <see cref="IWireWritable"/> or carries this attribute itself. A
/// <see langword="string"/> needs <see cref="WireStringAttribute"/>, a list <see cref="WireArrayAttribute"/>, and
/// <see cref="WireAsAttribute"/> gives a field another width on the wire. A message whose layout depends on its values
/// (a field present or not, a list padded to a count, a loop with state) keeps a hand-written implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class WireMessageAttribute : Attribute
{
    public WireMessageAttribute()
    {
    }

    public WireMessageAttribute(WireMethods methods)
    {
        Methods = methods;
    }

    public WireMethods Methods { get; }
}

/// <summary>
/// How a <see langword="string"/> field goes on the wire.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class WireStringAttribute(WireStringFormat format) : Attribute
{
    public WireStringFormat Format { get; } = format;

    /// <summary>
    /// The characters of a <see cref="WireStringFormat.AsciiWithoutTerminator"/> string, which carries no length.
    /// </summary>
    public int Length { get; init; }
}

/// <summary>
/// A list field: an array, <c>List&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c> or <c>IReadOnlyCollection&lt;T&gt;</c>. Its count
/// goes first in <see cref="Prefix"/>, or, with <see cref="Count"/>, the list always has that many elements and no count goes.
/// The elements follow the rules of a field; <see cref="ElementString"/> and <see cref="ElementAs"/> stand for the attributes
/// an element cannot carry. A missing list is written as an empty one.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class WireArrayAttribute : Attribute
{
    public WireArrayAttribute()
    {
    }

    public WireArrayAttribute(WireLength prefix)
    {
        Prefix = prefix;
        HasPrefix = true;
    }

    public WireLength Prefix { get; }

    public bool HasPrefix { get; }

    /// <summary>
    /// A list without a count on the wire: always this many elements.
    /// </summary>
    public int Count { get; init; }

    public WireStringFormat ElementString { get; init; }

    public Type? ElementAs { get; init; }
}

/// <summary>
/// The field goes on the wire as this primitive type (<see langword="byte"/>, <see langword="short"/>,
/// <see langword="int"/>, ...): an enum wider or narrower than its underlying type, a flag sent as an <see langword="int"/>,
/// a number kept wider than the client sends it. A <see langword="bool"/> becomes 1 or 0 and is read as not zero.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class WireAsAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

/// <summary>
/// An auto-property that is not on the wire.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class WireIgnoreAttribute : Attribute;
