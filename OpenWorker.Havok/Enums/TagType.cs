namespace OpenWorker.Havok.Enums;

/// <summary>
///     Magic marker constants used in the file.
/// </summary>
public enum TagType : sbyte
{
    Eof = -1,

    /// <summary>
    ///     Invalid tag.
    /// </summary>
    None = 0,

    /// <summary>
    ///     File header info. Followed by an integer version number.
    ///     The rest of the header data is determined by the version number.
    /// </summary>
    FileInfo = 1,

    /// <summary>
    ///     The following item is an hkGenericClass
    /// </summary>
    Metadata = 2,

    /// <summary>
    ///     The following item is an hkGenericObject which will not be
    ///     referenced again.
    /// </summary>
    Object = 3,

    /// <summary>
    ///     The following item is an object which may be referenced again
    ///     from TAG_OBJECT_BACKREF. The id is implicitly the count of preceding
    ///     remembered objects.
    /// </summary>
    ObjectRemember = 4,

    /// <summary>
    ///     Refer to a previously encountered object.
    ///     Followed by the integer object index.
    /// </summary>
    ObjectBackref = 5,

    /// <summary>
    ///     The null object pointer, only used in version 0 and 1
    /// </summary>
    ObjectNull = 6,

    /// <summary>
    ///     End of file marker
    /// </summary>
    FileEnd = 7
}