namespace OpenWorker.Havok;

internal enum HavokSubType
{
    Invalid,
    Void,
    Byte,
    Real,					
    Int,
    String,
    Class,
    Pointer,
    Array,
    Tuple,
    CountOf,
}

public class HavokTypeManager : HavokReferencedObject
{
}