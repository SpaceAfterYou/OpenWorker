using OpenWorker.Havok.Attributes;

namespace OpenWorker.Havok;

[HavokSerializeClass("hkReferencedObject", 0)]
public class HavokReferencedObject : HavokBaseObject
{
    [HavokSerializeMember("memSizeAndFlags")]
    public short MemorySizeAndFlags { get; set; }

    [HavokSerializeMember("referenceCount")]
    public short ReferenceCount { get; set; }
}