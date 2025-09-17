namespace OpenWorker.Havok.Enums;

/// <summary>
/// hkDataWorld supported types.
/// </summary>
public enum DataWorldType
{
    /// <summary>
    /// Native type. hkDataWorld contains wrapped native
    /// object and hkClass pointers that are represented
    /// by hkDataObject and hkDataClass respectively.
    /// See hkDataWorldNative for details.
    /// </summary>
    TypeNative,

    /// <summary>
    /// Dictionary type. hkDataWorld contains instances of
    /// hkDataObject described by hkDataClass.
    /// See hkDataWorldDict for details.
    /// </summary>
    Type
}