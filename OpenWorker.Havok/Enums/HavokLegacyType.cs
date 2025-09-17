namespace OpenWorker.Havok.Enums;

public enum HavokLegacyType
{
    /// <summary>
    /// Not reflected.
    /// </summary>
    Void = 0,

    /// <summary>
    /// Byte, signed or unsigned 8-bit integer, used only in arrays and tuples
    /// </summary>
    Byte,
    /// <summary>
    /// Signed or unsigned 8/16/32/64-bit integer.
    /// </summary>
    Int,
    /// <summary>
    /// 32-bit float.
    /// </summary>
    Real,
    /// <summary>
    /// Fixed array of 4 REAL (e.g., hkVector, hkQuaternion).
    /// </summary>
    Vec4,
    /// <summary>
    /// Fixed array of 8 REAL.
    /// </summary>
    Vec8,
    /// <summary>
    /// Fixed array of 12 REAL (e.g., hkMatrix3, hkQsTransform, hkRotation).
    /// </summary>
    Vec12,
    /// <summary>
    /// Fixed array of 16 REAL (e.g., hkTransform, hkMatrix4).
    /// </summary>
    Vec16,
    /// <summary>
    /// hkDataObject.
    /// </summary>
    Object,
    /// <summary>
    /// hkDataObject (embedded struct data).
    /// </summary>
    Struct,
    /// <summary>
    /// C-style string.
    /// </summary>
    CString,
    /// <summary>
    /// The number of basic hkDataObject types.
    /// </summary>
    NumBasicTypes,
    /// <summary>
    /// Mask for the basic hkDataObject types.
    /// </summary>
    MaskBasicTypes = 0xf,

    /// <summary>
    /// Bit indicating an array of the basic type data.
    /// </summary>
    Array = 0x10, // per object size array

    /// <summary>
    /// Array of BYTE.
    /// </summary>
    ArrayByte = Array | Byte,

    /// <summary>
    /// Array of INT.
    /// </summary>
    ArrayInt = Array | Int,

    /// <summary>
    /// Array of REAL.
    /// </summary>
    ArrayReal = Array | Real,

    /// <summary>
    /// Array of VEC_4.
    /// </summary>
    ArrayVec4 = Array | Vec4,

    /// <summary>
    /// Array of VEC_8.
    /// </summary>
    ArrayVec8 = Array | Vec8,

    /// <summary>
    /// Array of VEC_12.
    /// </summary>
    ArrayVec12 = Array | Vec12,

    /// <summary>
    /// Array of VEC_16.
    /// </summary>
    ArrayVec16 = Array | Vec16,

    /// <summary>
    /// Array of OBJECT.
    /// </summary>
    ArrayObject = Array | Object,

    /// <summary>
    /// Array of STRUCT.
    /// </summary>
    ArrayStruct = Array | Struct,

    /// <summary>
    /// Array of CSTRING.
    /// </summary>
    ArrayCString = Array | CString,

    /// <summary>
    /// Bit indicating a tuple of the basic type data.
    /// </summary>
    Tuple = 0x20, // fixed size array, size is per class

    /// <summary>
    /// Tuple of BYTE.
    /// </summary>
    TupleByte = Tuple | Byte,

    /// <summary>
    /// Tuple of INT.
    /// </summary>
    TupleInt = Tuple | Int,

    /// <summary>
    /// Tuple of REAL.
    /// </summary>
    TupleReal = Tuple | Real,

    /// <summary>
    /// Tuple of VEC_4.
    /// </summary>
    TupleVec4 = Tuple | Vec4,

    /// <summary>
    /// Tuple of VEC_8.
    /// </summary>
    TupleVec8 = Tuple | Vec8,

    /// <summary>
    /// Tuple of VEC_12.
    /// </summary>
    TupleVec12 = Tuple | Vec12,

    /// <summary>
    /// Tuple of VEC_16.
    /// </summary>
    TupleVec16 = Tuple | Vec16,

    /// <summary>
    /// Tuple of OBJECT.
    /// </summary>
    TupleObject = Tuple | Object,

    /// <summary>
    /// Tuple of STRUCT.
    /// </summary>
    TupleStruct = Tuple | Struct,

    /// <summary>
    /// Tuple of CSTRING.
    /// </summary>
    TupleCString = Tuple | CString,
}